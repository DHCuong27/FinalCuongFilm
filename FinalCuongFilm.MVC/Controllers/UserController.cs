using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
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


		// Bổ sung thêm _historyService nếu bạn có bảng lưu lịch sử xem phim

		// CHỈ GIỮ LẠI ĐÚNG 1 CONSTRUCTOR NÀY
		public UserController(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager, IFavoriteService favoriteService)
		{
			_userManager = userManager;
			_favoriteService = favoriteService;
			_context = context;
		}

		// GET: /User/Profile
		// 1. HÀM GET PROFILE (Phải lấy user ném ra View)
		public async Task<IActionResult> Profile()
		{
			// Lấy thông tin user ĐANG ĐĂNG NHẬP từ Database
			var user = await _userManager.GetUserAsync(User);

			if (user == null)
			{
				return RedirectToPage("/Account/Login", new { area = "Identity" });
			}

			// Quăng nguyên cục user này ra View để View đọc được AvatarUrl
			return View(user);
		}

		// GET: /User/MyList
		public async Task<IActionResult> MyList()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			// 1. Lấy danh sách FavoriteDto từ Database
			var favorites = await _favoriteService.GetUserFavoritesAsync(userId);

			// 2. Chuyển đổi (Map) dữ liệu từ FavoriteDto sang MovieDto
			var movies = favorites.Select(f => new MovieDto
			{
				Id = f.MovieId,               // Cực kỳ quan trọng: Để nút Remove tìm đúng ID phim để xóa
				Title = f.MovieTitle,         // Lấy tên phim
				Slug = f.MovieSlug,           // Lấy đường dẫn phim
				PosterUrl = f.MoviePosterUrl  // Lấy ảnh bìa phim
			}).ToList();

			ViewData["Title"] = "My List";

			// 3. Trả về View danh sách MovieDto chuẩn xịn
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
		public async Task<IActionResult> UpdateProfile(string FullName, string Bio, string FavoriteGenreId)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return NotFound();

			// Cập nhật thông tin (Giả sử ApplicationUser của bạn đã mở rộng các cột này)
			user.FullName = FullName;
			//user.Bio = Bio;
			//user.FavoriteGenreId = FavoriteGenreId; // Ghi nhận thể loại yêu thích

			await _userManager.UpdateAsync(user);

			TempData["ProfileSuccess"] = "Your profile has been updated!";
			return RedirectToAction("Profile");
		}
	}
}