using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	[Authorize] // Chỉ user đã login
	public class FavoritesController : Controller
	{
		private readonly IFavoriteService _favoriteService;
		private readonly IMovieService _movieService;

		public FavoritesController(
			IFavoriteService favoriteService,
			IMovieService movieService)
		{
			_favoriteService = favoriteService;
			_movieService = movieService;
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
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return Json(new { success = false, message = "Vui lòng đăng nhập!" });
			}

			try
			{
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
				return Json(new { success = false, message = ex.Message });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
			}
		}

		// POST: /Favorites/Remove/{movieId}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Remove(Guid movieId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return Json(new { success = false, message = "Vui lòng đăng nhập!" });
			}

			try
			{
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
				return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
			}
		}

		// GET: /Favorites/Toggle/{movieId} (AJAX)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Toggle(Guid movieId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return Json(new { success = false, message = "Vui lòng đăng nhập!" });
			}

			try
			{
				var isFavorite = await _favoriteService.IsFavoriteAsync(userId, movieId);

				if (isFavorite)
				{
					await _favoriteService.RemoveFavoriteAsync(userId, movieId);
					return Json(new
					{
						success = true,
						isFavorite = false,
						message = "Đã xóa khỏi danh sách yêu thích!"
					});
				}
				else
				{
					await _favoriteService.AddFavoriteAsync(userId, movieId);
					return Json(new
					{
						success = true,
						isFavorite = true,
						message = "Đã thêm vào danh sách yêu thích!"
					});
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: /Favorites/Check/{movieId} (AJAX)
		[HttpGet]
		public async Task<IActionResult> Check(Guid movieId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return Json(new { isFavorite = false });
			}

			var isFavorite = await _favoriteService.IsFavoriteAsync(userId, movieId);
			return Json(new { isFavorite });
		}
	}
}