using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.Service.Services
{
	public class MovieImportService : IMovieImportService
	{
		private readonly ITmdbService _tmdbService;
		private readonly CuongFilmDbContext _dbContext;

		public MovieImportService(ITmdbService tmdbService, CuongFilmDbContext dbContext)
		{
			_tmdbService = tmdbService;
			_dbContext = dbContext;
		}

		public async Task<(bool Success, string Message)> ImportMovieAsync(string title)
		{
			var searchResult = await _tmdbService.SearchMovieAsync(title);
			bool isTvSeries = false;

			if (searchResult == null)
			{
				searchResult = await _tmdbService.SearchTvShowAsync(title);
				isTvSeries = true;
			}

			if (searchResult == null)
				return (false, $"Film not found '{title}' on TMDB (both Movie and TV Show).");

			// Check if movie exists in DB
			var isExist = await _dbContext.Movies.AnyAsync(m => m.TmdbId == searchResult.Id && m.Type == (isTvSeries ? MovieType.Series : MovieType.Movie));
			if (isExist)
				return (false, $"Film '{searchResult.Title}' already exists in the system!");

			// Fetch movie details
			var movieDetails = isTvSeries
				? await _tmdbService.GetTvShowDetailsAsync(searchResult.Id)
				: await _tmdbService.GetMovieDetailsAsync(searchResult.Id);

			if (movieDetails == null)
				return (false, $"Unable to obtain film details for '{searchResult.Title}'.");

			using var transaction = await _dbContext.Database.BeginTransactionAsync();
			try
			{
				// 1. Initialize Movie Entity (Root of the graph)
				var movie = new Movie
				{
					Title = movieDetails.Title,
					Slug = SlugHelper.GenerateSlug(movieDetails.Title) + "-" + movieDetails.Id,
					Description = movieDetails.Overview,
					PosterUrl = !string.IsNullOrEmpty(movieDetails.PosterPath) ? "https://image.tmdb.org/t/p/w500" + movieDetails.PosterPath : null,
					TmdbId = movieDetails.Id,
					Status = MovieStatus.Completed,
					Type = isTvSeries ? MovieType.Series : MovieType.Movie,
					IsActive = true,
					DurationMinutes = movieDetails.Runtime,
					ReleaseYear = !string.IsNullOrEmpty(movieDetails.ReleaseDate) ? DateTime.Parse(movieDetails.ReleaseDate).Year : DateTime.Now.Year,

					// Initialize Collections to let EF Core handle Foreign Keys automatically
					MovieGenres = new List<MovieGenre>(),
					MovieActors = new List<MovieActor>()
				};

				// 2. Process Country Added Local cache & Slug check to prevent Unique Index Error)
				var firstCountry = movieDetails.ProductionCountries?.FirstOrDefault();
				if (firstCountry != null)
				{
					var countrySlug = SlugHelper.GenerateSlug(firstCountry.Name);

					var country = _dbContext.Countries.Local.FirstOrDefault(c => c.IsoCode == firstCountry.Iso_3166_1 || c.Slug == countrySlug)
								  ?? await _dbContext.Countries.FirstOrDefaultAsync(c => c.IsoCode == firstCountry.Iso_3166_1 || c.Slug == countrySlug);

					if (country == null)
					{
						country = new Country
						{
							Name = firstCountry.Name,
							IsoCode = firstCountry.Iso_3166_1,
							Slug = countrySlug
						};
					}
					else if (string.IsNullOrEmpty(country.IsoCode))
					{
						// Update IsoCode if the existing record in DB has a missing/null IsoCode
						country.IsoCode = firstCountry.Iso_3166_1;
					}

					movie.Country = country;
				}

				// 3. Process Genre - Bulk Read
				if (movieDetails.Genres != null && movieDetails.Genres.Any())
				{
					var genreSlugs = movieDetails.Genres.Select(g => SlugHelper.GenerateSlug(g.Name)).ToList();

					var existingGenres = await _dbContext.Genres
						.Where(g => genreSlugs.Contains(g.Slug))
						.ToListAsync();

					foreach (var genreData in movieDetails.Genres)
					{
						var slug = SlugHelper.GenerateSlug(genreData.Name);

						// Check Local cache as well to prevent duplicates in the same transaction
						var genre = _dbContext.Genres.Local.FirstOrDefault(g => g.Slug == slug)
									?? existingGenres.FirstOrDefault(g => g.Slug == slug);

						if (genre == null)
						{
							genre = new Genre { Name = genreData.Name, Slug = slug };
							existingGenres.Add(genre);
						}

						movie.MovieGenres.Add(new MovieGenre { Genre = genre });
					}
				}

				// 4. Process Actor - Bulk Read
				var credits = isTvSeries
					? await _tmdbService.GetTvCreditsAsync(movieDetails.Id)
					: await _tmdbService.GetMovieCreditsAsync(movieDetails.Id);

				if (credits != null && credits.Cast.Any())
				{
					var topCast = credits.Cast.Take(8).ToList();
					var castTmdbIds = topCast.Select(c => c.Id).ToList();

					var existingActors = await _dbContext.Actors
							.Where(a => a.TmdbId.HasValue && castTmdbIds.Contains(a.TmdbId.Value))
							.ToListAsync();

					foreach (var cast in topCast)
					{
						// FIXED: Added Local cache check for actors
						var actor = _dbContext.Actors.Local.FirstOrDefault(a => a.TmdbId == cast.Id)
									?? existingActors.FirstOrDefault(a => a.TmdbId == cast.Id);

						if (actor == null)
						{
							actor = new Actor
							{
								Name = cast.Name,
								Slug = SlugHelper.GenerateSlug(cast.Name) + "-" + cast.Id,
								AvartUrl = !string.IsNullOrEmpty(cast.ProfilePath) ? "https://image.tmdb.org/t/p/w300" + cast.ProfilePath : null,
								TmdbId = cast.Id,
								Gender = cast.Gender == 1 ? "Female" : (cast.Gender == 2 ? "Male" : "Unknown")
							};
							existingActors.Add(actor);
						}

						movie.MovieActors.Add(new MovieActor { Actor = actor });
					}
				}

				// 5. FINAL: Add Movie to DB and SaveChanges ONLY ONCE
				_dbContext.Movies.Add(movie);
				await _dbContext.SaveChangesAsync();
				await transaction.CommitAsync();

				return (true, $"Import successfully: {movie.Title} (Type: {movie.Type})");
			}
			catch (Exception)
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}