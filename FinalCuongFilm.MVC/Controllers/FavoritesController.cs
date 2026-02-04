using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Controllers
{
	[Authorize]
	public class FavoritesController : Controller
	{
		private readonly IFavoriteService _favoriteService;
		private readonly IMovieService _movieService;
		private readonly ILogger<FavoritesController> _logger;

		public FavoritesController(
			IFavoriteService favoriteService,
			IMovieService movieService,
			ILogger<FavoritesController> logger)
		{
			_favoriteService = favoriteService;
			_movieService = movieService;
			_logger = logger;
		}

		// GET: /Favorites
		public async Task<IActionResult> Index()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToAction("Login", "Account", new { area = "Identity" });
			}

			var favorites = await _favoriteService.GetUserFavoritesAsync(userId);

			ViewData["Title"] = "Phim yêu thích của tôi";
			return View(favorites);
		}

		// POST: /Favorites/Add/{movieId}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Add(Guid movieId)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null)
				{
					_logger.LogWarning("Add favorite: User not authenticated");
					return Json(new { success = false, message = "Vui lòng đăng nhập!" });
				}

				_logger.LogInformation("Adding favorite - UserId: {UserId}, MovieId: {MovieId}", userId, movieId);

				var favorite = await _favoriteService.AddFavoriteAsync(userId, movieId);

				return Json(new
				{
					success = true,
					message = "Đã thêm vào danh sách yêu thích!",
					data = favorite
				});
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Invalid operation when adding favorite");
				return Json(new { success = false, message = ex.Message });
			}
			catch (DbUpdateException dbEx)
			{
				var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
				_logger.LogError(dbEx, "DbUpdateException adding favorite. Error: {Error}", innerMessage);
				return Json(new { success = false, message = "Lỗi khi lưu vào database", details = innerMessage });
			}
			catch (Exception ex)
			{
				var innerMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Error adding favorite. Error: {Error}", innerMessage);
				return Json(new { success = false, message = "Có lỗi xảy ra: " + innerMessage });
			}
		}

		// POST: /Favorites/Remove/{movieId}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Remove(Guid movieId)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null)
				{
					return Json(new { success = false, message = "Vui lòng đăng nhập!" });
				}

				_logger.LogInformation("Removing favorite - UserId: {UserId}, MovieId: {MovieId}", userId, movieId);

				var success = await _favoriteService.RemoveFavoriteAsync(userId, movieId);

				if (success)
				{
					return Json(new
					{
						success = true,
						message = "Đã xóa khỏi danh sách yêu thích!"
					});
				}

				return Json(new { success = false, message = "Không tìm thấy!" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing favorite");
				return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
			}
		}

		// POST: /Favorites/Toggle/{movieId}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Toggle(Guid movieId)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null)
				{
					_logger.LogWarning("Toggle favorite: User not authenticated");
					return Json(new { success = false, message = "Vui lòng đăng nhập!" });
				}

				_logger.LogInformation("Toggling favorite - UserId: {UserId}, MovieId: {MovieId}", userId, movieId);

				var isFavorite = await _favoriteService.IsFavoriteAsync(userId, movieId);

				if (isFavorite)
				{
					// Remove
					var removeSuccess = await _favoriteService.RemoveFavoriteAsync(userId, movieId);
					if (removeSuccess)
					{
						return Json(new
						{
							success = true,
							isFavorite = false,
							message = "Đã xóa khỏi danh sách yêu thích!"
						});
					}
				}
				else
				{
					// Add
					var favorite = await _favoriteService.AddFavoriteAsync(userId, movieId);
					return Json(new
					{
						success = true,
						isFavorite = true,
						message = "Đã thêm vào danh sách yêu thích!"
					});
				}

				return Json(new { success = false, message = "Không thể thực hiện thao tác!" });
			}
			catch (InvalidOperationException ex)
			{
				_logger.LogWarning(ex, "Invalid operation when toggling favorite");
				return Json(new { success = false, message = ex.Message });
			}
			catch (DbUpdateException dbEx)
			{
				var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
				_logger.LogError(dbEx, "DbUpdateException toggling favorite. Error: {Error}", innerMessage);
				return Json(new { success = false, message = "Lỗi database", details = innerMessage });
			}
			catch (Exception ex)
			{
				var innerMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Error toggling favorite. Error: {Error}", innerMessage);
				return Json(new { success = false, message = "Có lỗi xảy ra: " + innerMessage });
			}
		}
	}
}