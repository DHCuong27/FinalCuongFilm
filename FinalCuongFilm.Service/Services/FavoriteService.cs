using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class FavoriteService : IFavoriteService
	{
		private readonly CuongFilmDbContext _context;
		private readonly UserManager<CuongFilmUser> _userManager;

		public FavoriteService(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		public async Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId)
		{
			var favorites = await _context.Favorites
				.Include(f => f.Movie)
				.Where(f => f.UserId == userId)
				.OrderByDescending(f => f.CreatedAt)
				.ToListAsync();

			// Lấy username từ UserManager
			var user = await _userManager.FindByIdAsync(userId);
			var userName = user?.UserName ?? "Unknown";

			return favorites.Select(f => new FavoriteDto
			{
				Id = f.Id,
				UserId = f.UserId,
				UserName = userName,
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

			//  Check user exists
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null)
			{
				throw new InvalidOperationException("User không tồn tại!");
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
				UserName = user.UserName ?? "Unknown",
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

		// 1. Hàm lấy danh sách lịch sử xem
		public async Task<IEnumerable<MovieDto>> GetUserWatchHistoryAsync(string userId)
		{
			var history = await _context.WatchHistories // Đảm bảo bạn đã tạo bảng WatchHistories trong DbContext
				.Include(h => h.Movie) // Bắt buộc Include để lấy được Data của phim
				.Where(h => h.UserId == userId)
				.OrderByDescending(h => h.LastWatchedAt) // Sắp xếp phim mới xem nhất lên đầu
				.Select(h => new MovieDto
				{
					Id = h.Movie.Id,
					Title = h.Movie.Title,
					Slug = h.Movie.Slug,
					PosterUrl = h.Movie.PosterUrl
				})
				.ToListAsync();

			return history;
		}

		// 2. Hàm lưu/cập nhật lịch sử (Gọi hàm này ở MovieController khi bấm xem phim)
		public async Task SaveWatchHistoryAsync(string userId, Guid movieId)
		{
			// Kiểm tra xem user này đã từng xem bộ phim này chưa
			var existingHistory = await _context.WatchHistories
				.FirstOrDefaultAsync(h => h.UserId == userId && h.MovieId == movieId);

			if (existingHistory != null)
			{
				// Nếu đã xem rồi, chỉ cần cập nhật lại thời gian xem thành hiện tại
				existingHistory.LastWatchedAt = DateTime.UtcNow;
			}
			else
			{
				// Nếu chưa xem bao giờ, tạo record lịch sử mới
				var newHistory = new WatchHistory
				{
					UserId = userId,
					MovieId = movieId,
					LastWatchedAt = DateTime.UtcNow
				};
				_context.WatchHistories.Add(newHistory);
			}

			// Lưu thay đổi vào Database
			await _context.SaveChangesAsync();
		}
	}
}