using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	[Authorize] // Đặt Authorize ở đầu Class, mọi hàm bên trong đều tự động được bảo vệ!
	public class UserController : Controller
	{
		private readonly IFavoriteService _favoriteService;
		private readonly UserManager<CuongFilmUser> _userManager;
		private readonly CuongFilmDbContext _context;
		private readonly IVipService _vipService;


		// Bổ sung thêm _historyService nếu bạn có bảng lưu lịch sử xem phim


		public UserController(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager, IVipService vipService, IFavoriteService favoriteService)
		{
			_userManager = userManager;
			_favoriteService = favoriteService;
			_context = context;
			_vipService = vipService;
		}

		// GET: /User/Profile
		// 1. HÀM GET PROFILE (Phải lấy user ném ra View)
		[HttpGet]
		public async Task<IActionResult> Profile()
		{
			// 1. Get the currently logged-in user from the Database
			var user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				return RedirectToPage("/Account/Login", new { area = "Identity" });
			}

			// 2. CRITICAL FIX: Fetch the active VIP subscription using the user's ID
			var currentVip = await _vipService.GetCurrentUserSubscriptionAsync(user.Id);

			// 3. Pass the VIP data to the View via ViewBag
			ViewBag.CurrentVip = currentVip;

			// 4. Return the View WITH the user model so AvatarUrl and FullName work perfectly
			return View(user);
		}

		// GET: /User/MyList
		public async Task<IActionResult> MyList()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });


			var favorites = await _favoriteService.GetUserFavoritesAsync(userId);

			var movies = favorites.Select(f => new MovieDto
			{
				Id = f.MovieId,              
				Title = f.MovieTitle,        
				Slug = f.MovieSlug,           
				PosterUrl = f.MoviePosterUrl
			}).ToList();

			ViewData["Title"] = "My List";

			return View(movies);
		}

		// GET: /User/ContinueWatching
		public async Task<IActionResult> ContinueWatching()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			// Gọi hàm từ FavoriteService vừa tạo
			var historyMovies = await _favoriteService.GetUserWatchHistoryAsync(userId);

			ViewData["Title"] = "Watch History";
			return View(historyMovies); // Quăng danh sách ra View
		}

		[HttpPost]
		public async Task<IActionResult> ChangePassword(string OldPassword, string NewPassword, string ConfirmPassword)
		{
			if (NewPassword != ConfirmPassword)
			{
				TempData["PasswordError"] = "New password and confirmation do not match.";
				return RedirectToAction("Profile");
			}

			// Lấy user đang đăng nhập
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");

			// Đổi pass qua Identity
			var changePasswordResult = await _userManager.ChangePasswordAsync(user, OldPassword, NewPassword);

			if (!changePasswordResult.Succeeded)
			{
				// Có thể lấy lỗi chi tiết từ changePasswordResult.Errors
				TempData["PasswordError"] = "Incorrect current password or password does not meet requirements.";
				return RedirectToAction("Profile");
			}

			TempData["PasswordSuccess"] = "Your password has been changed successfully.";
			return RedirectToAction("Profile");
		}

		// 2. HÀM LƯU AVATAR (Có thêm check lỗi DB)
		[HttpPost]
		public async Task<IActionResult> UpdateAvatar(string? SelectedAvatarUrl, IFormFile? AvatarFile)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			string finalAvatarUrl = user.AvatarUrl ?? "/img/avatar.jpg"; // Đổi đường dẫn default cho chuẩn

			// Ưu tiên 1: Người dùng upload file từ máy
			if (AvatarFile != null && AvatarFile.Length > 0)
			{
				var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(AvatarFile.FileName)}";
				var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");

				if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

				var filePath = Path.Combine(uploadsFolder, fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await AvatarFile.CopyToAsync(stream);
				}

				finalAvatarUrl = $"/images/avatars/{fileName}";
			}
			// Ưu tiên 2: Chọn ảnh có sẵn từ Modal
			else if (!string.IsNullOrEmpty(SelectedAvatarUrl))
			{
				// Chỉnh sửa lại chuỗi đường dẫn nếu HTML đang dùng ký tự '~'
				finalAvatarUrl = SelectedAvatarUrl.Replace("~", "");
			}

			// Gán URL mới
			user.AvatarUrl = finalAvatarUrl;

			// Lưu vào Database
			var result = await _userManager.UpdateAsync(user);

			// KIỂM TRA LỖI NẾU LƯU THẤT BẠI
			if (!result.Succeeded)
			{
				TempData["PasswordError"] = "Failed to update avatar in database.";
				return RedirectToAction("Profile");
			}

			TempData["ProfileSuccess"] = "Avatar updated successfully!";
			return RedirectToAction("Profile");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateProfile(string FullName)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			// Check if the input is not empty or just spaces
			if (!string.IsNullOrWhiteSpace(FullName))
			{
				user.FullName = FullName.Trim();
				var result = await _userManager.UpdateAsync(user);

				if (result.Succeeded)
				{
					TempData["ProfileSuccess"] = "Your profile has been updated successfully!";
				}
				else
				{
					TempData["PasswordError"] = "Failed to update profile. Please try again.";
				}
			}
			else
			{
				TempData["PasswordError"] = "Full name cannot be empty.";
			}

			return RedirectToAction("Profile");
		}
	}
}