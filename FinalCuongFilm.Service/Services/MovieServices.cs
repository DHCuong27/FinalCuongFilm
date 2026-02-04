using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
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
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Movie_Actors)
					.ThenInclude(ma => ma.Actor)
				.OrderByDescending(m => m.CreatedAt)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Movie_Actors)
					.ThenInclude(ma => ma.Actor)
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
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Include(m => m.Movie_Actors)
					.ThenInclude(ma => ma.Actor)
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

		public async Task<MovieDto> CreateAsync(MovieCreateDto dto)
		{
			var movie = new Movie
			{
				Title = dto.Title,
				Slug = GenerateSlug(dto.Title),
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
				ViewCount = 0,
				CreatedAt = DateTime.UtcNow
			};

			_context.Movies.Add(movie);
			await _context.SaveChangesAsync();

			// Add genres
			if (dto.GenreIds != null && dto.GenreIds.Any())
			{
				foreach (var genreId in dto.GenreIds)
				{
					_context.Movie_Genres.Add(new Movie_Genre
					{
						MovieId = movie.Id,
						GenreId = genreId
					});
				}
				await _context.SaveChangesAsync();
			}

			// Add actors
			if (dto.ActorIds != null && dto.ActorIds.Any())
			{
				foreach (var actorId in dto.ActorIds)
				{
					_context.Movie_Actors.Add(new Movie_Actor
					{
						MovieId = movie.Id,
						ActorId = actorId
					});
				}
				await _context.SaveChangesAsync();
			}

			return await GetByIdAsync(movie.Id) ?? throw new Exception("Failed to retrieve created movie");
		}

		public async Task<bool> UpdateAsync(MovieUpdateDto dto)
		{
			var movie = await _context.Movies.FindAsync(dto.Id);
			if (movie == null)
				return false;

			movie.Title = dto.Title;
			movie.Slug = GenerateSlug(dto.Title);
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

		public async Task<IEnumerable<MovieDto>> SearchAsync(string keyword)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.Title.Contains(keyword) ||
						   m.Description!.Contains(keyword))
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.Movie_Genres.Any(mg => mg.GenreId == genreId))
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<IEnumerable<MovieDto>> GetByCountryAsync(Guid countryId)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.CountryId == countryId)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.CreatedAt)
				.Take(count)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		public async Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12)
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Genres)
					.ThenInclude(mg => mg.Genre)
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(count)
				.ToListAsync();

			return movies.Select(m => MapToDto(m));
		}

		// ✅ HELPER METHODS
		private static string GenerateSlug(string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return string.Empty;

			// Convert to lowercase
			string slug = title.ToLowerInvariant();

			// Remove Vietnamese accents
			slug = RemoveVietnameseAccents(slug);

			// Replace spaces with hyphens
			slug = slug.Replace(" ", "-");

			// Remove invalid characters
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

			// Remove duplicate hyphens
			slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

			// Trim hyphens from start and end
			slug = slug.Trim('-');

			return slug;
		}

		private static string RemoveVietnameseAccents(string text)
		{
			string[] vietnameseChars = new string[]
			{
				"aAeEoOuUiIdDyY",
				"áàạảãâấầậẩẫăắằặẳẵ",
				"ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
				"éèẹẻẽêếềệểễ",
				"ÉÈẸẺẼÊẾỀỆỂỄ",
				"óòọỏõôốồộổỗơớờợởỡ",
				"ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
				"úùụủũưứừựửữ",
				"ÚÙỤỦŨƯỨỪỰỬỮ",
				"íìịỉĩ",
				"ÍÌỊỈĨ",
				"đ",
				"Đ",
				"ýỳỵỷỹ",
				"ÝỲỴỶỸ"
			};

			for (int i = 1; i < vietnameseChars.Length; i++)
			{
				for (int j = 0; j < vietnameseChars[i].Length; j++)
				{
					text = text.Replace(vietnameseChars[i][j], vietnameseChars[0][i - 1]);
				}
			}

			return text;
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

		public Task<bool> ExistsAsync(Guid id)
		{
			throw new NotImplementedException();
		}
	}
}