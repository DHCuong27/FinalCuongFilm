using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;
using System;
using System.Linq;
using System.Threading.Tasks;

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

			// Kiểm tra trùng trong DB
			var isExist = await _dbContext.Movies.AnyAsync(m => m.TmdbId == searchResult.Id && m.Type == (isTvSeries ? MovieType.Series : MovieType.Movie));
			if (isExist)
				return (false, $"Film '{searchResult.Title}' It already exists in the system!");

			// BƯỚC 2: Lấy chi tiết
			var movieDetails = isTvSeries
				? await _tmdbService.GetTvShowDetailsAsync(searchResult.Id)
				: await _tmdbService.GetMovieDetailsAsync(searchResult.Id);

			if (movieDetails == null)
				return (false, $"Unable to obtain film details. '{searchResult.Title}'.");

			using var transaction = await _dbContext.Database.BeginTransactionAsync();
			try
			{
				// A.import country
				Guid? countryId = null;
				var firstCountry = movieDetails.ProductionCountries?.FirstOrDefault();
				if (firstCountry != null)
				{
					var country = await _dbContext.Countries.FirstOrDefaultAsync(c => c.IsoCode == firstCountry.Iso31661);
					if (country == null)
					{
						country = new Country
						{
							Name = firstCountry.Name,
							IsoCode = firstCountry.Iso31661,
							Slug = SlugHelper.GenerateSlug(firstCountry.Name)
						};
						_dbContext.Countries.Add(country);
						await _dbContext.SaveChangesAsync();
					}
					countryId = country.Id;
				}

				// B. create movie
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
					CountryId = countryId,
					DurationMinutes = movieDetails.Runtime,
					ReleaseYear = !string.IsNullOrEmpty(movieDetails.ReleaseDate) ? DateTime.Parse(movieDetails.ReleaseDate).Year : DateTime.Now.Year
				};

				_dbContext.Movies.Add(movie);
				await _dbContext.SaveChangesAsync();

				// C. imprt genre
				if (movieDetails.Genres != null)
				{
					foreach (var genreData in movieDetails.Genres)
					{
						var genreSlug = SlugHelper.GenerateSlug(genreData.Name);
						var genre = await _dbContext.Genres.FirstOrDefaultAsync(g => g.Slug == genreSlug);
						if (genre == null)
						{
							genre = new Genre { Name = genreData.Name, Slug = genreSlug };
							_dbContext.Genres.Add(genre);
							await _dbContext.SaveChangesAsync();
						}
						_dbContext.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = genre.Id });
					}
				}

				// D. import actor
				var credits = isTvSeries
					? await _tmdbService.GetTvCreditsAsync(movieDetails.Id)
					: await _tmdbService.GetMovieCreditsAsync(movieDetails.Id);

				if (credits != null && credits.Cast.Any())
				{
					foreach (var cast in credits.Cast.Take(8))
					{
						var actor = await _dbContext.Actors.FirstOrDefaultAsync(a => a.TmdbId == cast.Id);
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
							_dbContext.Actors.Add(actor);
							await _dbContext.SaveChangesAsync();
						}
						_dbContext.MovieActors.Add(new MovieActor { MovieId = movie.Id, ActorId = actor.Id });
					}
				}

				await _dbContext.SaveChangesAsync();
				await transaction.CommitAsync();

				return (true, $"Import thành công phim: {movie.Title} (Loại: {movie.Type})");
			}
			catch (Exception)
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}