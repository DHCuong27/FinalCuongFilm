using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Common.Helpers;
using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class MovieService : IMovieService
	{
		private readonly CuongFilmDbContext _context;

		public MovieService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<MovieDto>> GetAllAsync()
		{
			var movies = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Actors).ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.ToListAsync(); // ✅ ToList TRƯỚC

			// ✅ SAU ĐÓ mới map
			return movies.Select(m => MapToDto(m)).ToList();
		}

		public async Task<MovieDto?> GetByIdAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.Country)
				.Include(m => m.Language)
				.Include(m => m.Movie_Actors).ThenInclude(ma => ma.Actor)
				.Include(m => m.Movie_Genres).ThenInclude(mg => mg.Genre)
				.FirstOrDefaultAsync(m => m.Id == id);

			return movie == null ? null : MapToDto(movie);
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
			foreach (var actorId in dto.ActorIds)
			{
				_context.Movie_Actors.Add(new Movie_Actor
				{
					MovieId = movie.Id,
					ActorId = actorId
				});
			}

			// Thêm genres
			foreach (var genreId in dto.GenreIds)
			{
				_context.Movie_Genres.Add(new Movie_Genre
				{
					MovieId = movie.Id,
					GenreId = genreId
				});
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

			// Cập nhật actors (xóa cũ, thêm mới)
			_context.Movie_Actors.RemoveRange(movie.Movie_Actors);
			foreach (var actorId in dto.ActorIds)
			{
				_context.Movie_Actors.Add(new Movie_Actor
				{
					MovieId = movie.Id,
					ActorId = actorId
				});
			}

			// Cập nhật genres (xóa cũ, thêm mới)
			_context.Movie_Genres.RemoveRange(movie.Movie_Genres);
			foreach (var genreId in dto.GenreIds)
			{
				_context.Movie_Genres.Add(new Movie_Genre
				{
					MovieId = movie.Id,
					GenreId = genreId
				});
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var movie = await _context.Movies
				.Include(m => m.Movie_Actors)
				.Include(m => m.Movie_Genres)
				.Include(m => m.Episodes)
				.FirstOrDefaultAsync(m => m.Id == id);

			if (movie == null)
				return false;

			// Kiểm tra nghiệp vụ: không cho xóa nếu có episodes
			if (movie.Episodes.Any())
			{
				throw new InvalidOperationException("Không thể xóa phim đã có tập phim. Vui lòng xóa tất cả tập phim trước.");
			}

			_context.Movies.Remove(movie);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Movies.AnyAsync(m => m.Id == id);
		}

		// ✅ CHUYỂN THÀNH STATIC METHOD
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
				Type = movie.Type,
				Status = movie.Status,
				IsActive = movie.IsActive,
				LanguageId = movie.LanguageId,
				CountryId = movie.CountryId,
				CountryName = movie.Country?.Name,
				LanguageName = movie.Language?.Name,
				SelectedActorIds = movie.Movie_Actors?.Select(ma => ma.ActorId).ToList() ?? new List<Guid>(),
				SelectedGenreIds = movie.Movie_Genres?.Select(mg => mg.GenreId).ToList() ?? new List<Guid>()
			};
		}
	}
}