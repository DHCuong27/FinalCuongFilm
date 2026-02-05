using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinalCuongFilm.Service.Services
{
	public class MovieService : IMovieService
	{
		private readonly CuongFilmDbContext _context;
		private readonly ILogger<MovieService> _logger;

		public MovieService(CuongFilmDbContext context, ILogger<MovieService> logger)
		{
			_context = context;
			_logger = logger;
		}

		public async Task<IEnumerable<MovieDto>> GetAllAsync()
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Actors).ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.OrderByDescending(m => m.CreatedAt)
				.ToListAsync();

			return movies.Select(m => MapToDto(m)).ToList();
		}

		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Actors).ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.FirstOrDefaultAsync(m => m.Id == id);

			return movie == null ? null : MapToDto(movie);
		}

		// ✅ THÊM METHOD MỚI
		public async Task<MovieDto?> GetBySlugAsync(string slug)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Actors).ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.FirstOrDefaultAsync(m => m.Slug == slug);

			return movie == null ? null : MapToDto(movie);
		}

		// ✅ THÊM METHOD MỚI
		public async Task<bool> IncrementViewCountAsync(Guid id)
		{
			try
			{
				var movie = await _context.Movies.FindAsync(id);
				if (movie == null)
					return false;

				movie.ViewCount++;
				await _context.SaveChangesAsync();

				_logger.LogInformation("Incremented view count for movie {MovieId}. New count: {ViewCount}",
					id, movie.ViewCount);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error incrementing view count for movie {MovieId}", id);
				return false;
			}
		}

		// ✅ THÊM METHOD MỚI
		public async Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.Take(count)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		// ✅ THÊM METHOD MỚI
		public async Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(count)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		// ✅ THÊM METHOD MỚI
		public async Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.Include(m => m.Reviews)
				.Include(m => m.Favorites)
				.Where(m => m.Movie_Genres.Any(mg => mg.GenreId == genreId) && m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
		{
			var slug = SlugHelper.GenerateSlug(dto.Title);

			var movie = new Movie
			{
				Id = Guid.NewGuid(),
				Title = dto.Title,
				Slug = slug,
				Description = dto.Description,
				ReleaseYear = dto.ReleaseYear,
				DurationMinutes = dto.DurationMinutes,
				PosterUrl = dto.PosterUrl,
				TrailerUrl = dto.TrailerUrl,
				Type = dto.Type,
				Status = dto.Status,
				IsActive = dto.IsActive,
				LanguageId = dto.LanguageId,
				CountryId = dto.CountryId,
				CreatedAt = DateTime.UtcNow,
				ViewCount = 0
			};

			_context.Movies.Add(movie);

			// Thêm actors
			if (dto.ActorIds != null)
			{
				foreach (var actorId in dto.ActorIds)
				{
					_context.Movie_Actors.Add(new Movie_Actor
					{
						MovieId = movie.Id,
						ActorId = actorId
					});
				}
			}

			// Thêm genres
			if (dto.GenreIds != null)
			{
				foreach (var genreId in dto.GenreIds)
				{
					_context.Movie_Genres.Add(new Movie_Genre
					{
						MovieId = movie.Id,
						GenreId = genreId
					});
				}
			}

			await _context.SaveChangesAsync();

			return await GetByIdAsync(movie.Id) ?? throw new Exception("Failed to create movie");
		}

		public async Task<bool> UpdateAsync(MovieUpdateDto dto)
		{
			var movie = await _context.Movies
				.Include(m => m.Movie_Actors)
				.Include(m => m.Movie_Genres)
				.FirstOrDefaultAsync(m => m.Id == dto.Id);

			if (movie == null)
				return false;

			var slug = SlugHelper.GenerateSlug(dto.Title);

			movie.Title = dto.Title;
			movie.Slug = slug;
			movie.Description = dto.Description;
			movie.ReleaseYear = dto.ReleaseYear;
			movie.DurationMinutes = dto.DurationMinutes;
			movie.PosterUrl = dto.PosterUrl;
			movie.TrailerUrl = dto.TrailerUrl;
			movie.Type = dto.Type;
			movie.Status = dto.Status;
			movie.IsActive = dto.IsActive;
			movie.LanguageId = dto.LanguageId;
			movie.CountryId = dto.CountryId;
			movie.UpdatedAt = DateTime.UtcNow;

			// Update actors
			if (dto.ActorIds != null)
			{
				var existingActors = _context.Movie_Actors.Where(ma => ma.MovieId == movie.Id);
				_context.Movie_Actors.RemoveRange(existingActors);

				foreach (var actorId in dto.ActorIds)
				{
					_context.Movie_Actors.Add(new Movie_Actor
					{
						MovieId = movie.Id,
						ActorId = actorId
					});
				}
			}

			// Update genres
			if (dto.GenreIds != null)
			{
				var existingGenres = _context.Movie_Genres.Where(mg => mg.MovieId == movie.Id);
				_context.Movie_Genres.RemoveRange(existingGenres);

				foreach (var genreId in dto.GenreIds)
				{
					_context.Movie_Genres.Add(new Movie_Genre
					{
						MovieId = movie.Id,
						GenreId = genreId
					});
				}
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var movie = await _context.Movies.FindAsync(id);
			if (movie == null)
				return false;

			_context.Movies.Remove(movie);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Movies.AnyAsync(m => m.Id == id);
		}

		private static MovieDto MapToDto(Movie movie)
		{
			return new MovieDto
			{
				Id = movie.Id,
				Title = movie.Title,
				Slug = movie.Slug,
				Description = movie.Description,
				ReleaseYear = movie.ReleaseYear,
				DurationMinutes = movie.DurationMinutes,
				PosterUrl = movie.PosterUrl,
				TrailerUrl = movie.TrailerUrl,
				ViewCount = movie.ViewCount,
				Type = movie.Type,
				Status = movie.Status,
				IsActive = movie.IsActive,
				//CreatedAt = movie.CreatedAt,
				//UpdatedAt = movie.UpdatedAt,

				LanguageId = movie.LanguageId,
				LanguageName = movie.Language?.Name,

				CountryId = movie.CountryId,
				CountryName = movie.Country?.Name,

				SelectedGenreIds = movie.Movie_Genres?.Select(mg => mg.GenreId).ToList() ?? new List<Guid>(),
				//GenreNames = movie.Movie_Genres?.Select(mg => mg.Genre.Name).ToList() ?? new List<string>(),

				SelectedActorIds = movie.Movie_Actors?.Select(ma => ma.ActorId).ToList() ?? new List<Guid>(),
				//ActorNames = movie.Movie_Actors?.Select(ma => ma.Actor.Name).ToList() ?? new List<string>(),

				//AverageRating = movie.Reviews?.Any() == true
				//	? Math.Round(movie.Reviews.Where(r => r.IsApproved).Average(r => r.Rating), 1)
				//	: 0,
				//TotalReviews = movie.Reviews?.Count(r => r.IsApproved) ?? 0,
				//TotalFavorites = movie.Favorites?.Count ?? 0
			};
		}
	}
}