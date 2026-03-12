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

		// Đổi kiểu trả về thành Tuple để báo lỗi chi tiết ra UI
		public async Task<(bool Success, string Message)> ImportMovieAsync(string title)
		{
			// 1. Tìm phim cơ bản để lấy TmdbId
			var searchResult = await _tmdbService.SearchMovieAsync(title);
			if (searchResult == null)
				return (false, $"No movie title was found. '{title}' on TMDB.");

			var isExist = await _dbContext.Movies.AnyAsync(m => m.TmdbId == searchResult.Id);
			if (isExist)
				return (false, $"Movie '{searchResult.Title}'It already exists in the system!");

			// 2. GỌI API LẤY CHI TIẾT PHIM (Để có Thể loại & Quốc gia)
			var movieDetails = await _tmdbService.GetMovieDetailsAsync(searchResult.Id);
			if (movieDetails == null)
				return (false, $"The film's details are unavailable. '{searchResult.Title}'.");

			using var transaction = await _dbContext.Database.BeginTransactionAsync();
			try
			{
				// 
				// A. XỬ LÝ QUỐC GIA (Chỉ lấy quốc gia đầu tiên làm đại diện)
				// 
				Guid? countryId = null;
				var firstCountry = movieDetails.Production_Countries?.FirstOrDefault();
				if (firstCountry != null)
				{
					var countryIso = firstCountry.Iso_3166_1;
					// Kiểm tra xem Quốc gia đã có trong Database chưa (dựa theo mã ISO)
					var country = await _dbContext.Countries.FirstOrDefaultAsync(c => c.IsoCode == countryIso);

					if (country == null)
					{
						country = new Country
						{
							Name = firstCountry.Name,
							IsoCode = countryIso,
							Slug = SlugHelper.GenerateSlug(firstCountry.Name)
						};
						_dbContext.Countries.Add(country);
						await _dbContext.SaveChangesAsync(); // Lưu để lấy ID
					}
					countryId = country.Id;
				}

				// 
				// B. TẠO MOVIE CHÍNH (Gán luôn CountryId vào đây)
				// 
				var movie = new Movie
				{
					Title = movieDetails.Title,
					Slug = SlugHelper.GenerateSlug(movieDetails.Title) + "-" + movieDetails.Id,
					Description = movieDetails.Overview,
					PosterUrl = !string.IsNullOrEmpty(movieDetails.Poster_Path) ? "https://image.tmdb.org/t/p/w500" + movieDetails.Poster_Path : null,
					TmdbId = movieDetails.Id,
					Status = MovieStatus.Completed,
					Type = MovieType.Movie,
					IsActive = true,
					CountryId = countryId // <-- Nối dữ liệu Quốc gia
				};

				_dbContext.Movies.Add(movie);
				await _dbContext.SaveChangesAsync(); // Lưu để EF Core sinh ra movie.Id

				// 
				// C. XỬ LÝ THỂ LOẠI (Nhiều - Nhiều)
				// 
				if (movieDetails.Genres != null && movieDetails.Genres.Any())
				{
					foreach (var genreData in movieDetails.Genres)
					{
						var genreSlug = SlugHelper.GenerateSlug(genreData.Name);

						// Kiểm tra xem Thể loại đã có trong Database chưa
						var genre = await _dbContext.Genres.FirstOrDefaultAsync(g => g.Slug == genreSlug);
						if (genre == null)
						{
							genre = new Genre
							{
								Name = genreData.Name,
								Slug = genreSlug
							};
							_dbContext.Genres.Add(genre);
							await _dbContext.SaveChangesAsync(); // Lưu để lấy ID
						}

						// Tạo mối quan hệ Movie - Genre
						_dbContext.MovieGenres.Add(new MovieGenre
						{
							MovieId = movie.Id,
							GenreId = genre.Id
						});
					}
				}

				// 
				// D. XỬ LÝ DIỄN VIÊN (Như cũ của bạn)
				// 
				var credits = await _tmdbService.GetMovieCreditsAsync(movieDetails.Id);
				if (credits != null && credits.Cast.Any())
				{
					var topCast = credits.Cast.Take(8).ToList();
					foreach (var cast in topCast)
					{
						var actor = await _dbContext.Actors.FirstOrDefaultAsync(a => a.TmdbId == cast.Id);
						if (actor == null)
						{
							actor = new Actor
							{
								Name = cast.Name,
								Slug = SlugHelper.GenerateSlug(cast.Name) + "-" + cast.Id,
								AvartUrl = !string.IsNullOrEmpty(cast.Profile_Path) ? "https://image.tmdb.org/t/p/w300" + cast.Profile_Path : null,
								TmdbId = cast.Id,
								Gender = cast.Gender == 1 ? "Female" : (cast.Gender == 2 ? "Male" : "Unknown")
							};
							_dbContext.Actors.Add(actor);
							await _dbContext.SaveChangesAsync();
						}

						_dbContext.MovieActors.Add(new MovieActor
						{
							MovieId = movie.Id,
							ActorId = actor.Id
						});
					}
				}

				// Chốt hạ toàn bộ xuống Database
				await _dbContext.SaveChangesAsync();
				await transaction.CommitAsync();

				return (true, $"Auto import '{movieDetails.Title}'Success (including Genre, Country, and Actors)!");
			}
			catch (Exception ex)
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}
}