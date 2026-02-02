using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class FavoriteService : IFavoriteService
	{
		private readonly CuongFilmDbContext _context;

		public FavoriteService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId)
		{
			var favorites = await _context.Favorites
				.Include(f => f.Movie)
				.Include(f => f.User)
				.Where(f => f.UserId == userId)
				.OrderByDescending(f => f.CreatedAt)
				.ToListAsync();

			return favorites.Select(f => new FavoriteDto
			{
				Id = f.Id,
				UserId = f.UserId,
				UserName = f.User.UserName ?? "",
				MovieId = f.MovieId,
				MovieTitle = f.Movie.Title,
				MoviePosterUrl = f.Movie.PosterUrl,
				CreatedAt = f.CreatedAt
			});
		}

		public async Task<bool> IsFavoriteAsync(string userId, Guid movieId)
		{
			return await _context.Favorites
				.AnyAsync(f => f.UserId == userId && f.MovieId == movieId);
		}

		public async Task<FavoriteDto> AddFavoriteAsync(string userId, Guid movieId)
		{
			// Check if already exists
			var existing = await _context.Favorites
				.FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

			if (existing != null)
			{
				throw new InvalidOperationException("Phim đã có trong danh sách yêu thích!");
			}

			// Check movie exists
			var movie = await _context.Movies.FindAsync(movieId);
			if (movie == null)
			{
				throw new KeyNotFoundException("Không tìm thấy phim!");
			}

			var favorite = new Favorite
			{
				UserId = userId,
				MovieId = movieId,
				CreatedAt = DateTime.UtcNow
			};

			_context.Favorites.Add(favorite);
			await _context.SaveChangesAsync();

			return new FavoriteDto
			{
				Id = favorite.Id,
				UserId = userId,
				MovieId = movieId,
				MovieTitle = movie.Title,
				MoviePosterUrl = movie.PosterUrl,
				CreatedAt = favorite.CreatedAt
			};
		}

		public async Task<bool> RemoveFavoriteAsync(string userId, Guid movieId)
		{
			var favorite = await _context.Favorites
				.FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId);

			if (favorite == null)
			{
				return false;
			}

			_context.Favorites.Remove(favorite);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<int> GetFavoriteCountAsync(Guid movieId)
		{
			return await _context.Favorites
				.CountAsync(f => f.MovieId == movieId);
		}
	}
}