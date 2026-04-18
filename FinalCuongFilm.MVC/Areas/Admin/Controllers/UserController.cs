using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UserController : Controller
	{
		private readonly UserManager<CuongFilmUser> _userManager;

		// XÓA BỎ RoleManager ở đây vì không cần dùng nữa
		private const string DefaultAvatarUrl = "/img/avatar.jpg";

		public UserController(UserManager<CuongFilmUser> userManager)
		{
			_userManager = userManager;
		}

		// GET: Admin/User
		public async Task<IActionResult> Index(string searchString, int page = 1)
		{
			int pageSize = 10;
			var query = _userManager.Users.AsQueryable();

			if (!string.IsNullOrWhiteSpace(searchString))
			{
				query = query.Where(u => u.FullName.Contains(searchString) || u.Email.Contains(searchString));
				ViewBag.SearchString = searchString;
			}

			int totalCount = await query.CountAsync();

			var users = await query.OrderByDescending(u => u.Id)
								   .Skip((page - 1) * pageSize)
								   .Take(pageSize)
								   .ToListAsync();

			var userDtos = new List<UserDto>();
			foreach (var user in users)
			{
				bool isActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

				userDtos.Add(new UserDto
				{
					Id = user.Id,
					FullName = user.FullName ?? user.UserName,
					Email = user.Email,
					AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl,
					Roles = await _userManager.GetRolesAsync(user), 
					IsActive = isActive,
					CreatedAt = user.CreatedAt
				});
			}

			var pagedResult = new PagedResult<UserDto>
			{
				Items = userDtos,
				TotalCount = totalCount,
				PageSize = pageSize,
				PageIndex = page
			};

			return View(pagedResult);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LockUser(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			var currentUser = await _userManager.GetUserAsync(User);
			if (currentUser != null && currentUser.Id == user.Id)
			{
				TempData["Error"] = "Bạn không thể tự khóa tài khoản của chính mình.";
				return RedirectToAction(nameof(Index));
			}

			await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
			TempData["Success"] = $"Đã khóa tài khoản {user.Email} thành công.";

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UnlockUser(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			await _userManager.SetLockoutEndDateAsync(user, null);
			TempData["Success"] = $"Đã mở khóa tài khoản {user.Email}.";

			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteUser(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			// 1. Find the target user
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			// 2. SELF-DELETION PROTECTION
			// Get the ID of the currently logged-in Admin
			var currentAdminId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			if (user.Id == currentAdminId)
			{
				TempData["Error"] = "Security Violation: You cannot delete your own administrative account.";
				return RedirectToAction(nameof(Index));
			}

			// 3. ADMIN PROTECTION (Optional but recommended)
			// If you want to prevent Admins from deleting other Admins
			var roles = await _userManager.GetRolesAsync(user);
			if (roles.Contains("Admin"))
			{
				// Special logic: Only allow deletion if there's another check or just block it
				// TempData["Error"] = "Action Denied: Administrative accounts cannot be deleted via this panel.";
				// return RedirectToAction(nameof(Index));
			}

			// 4. Execute Deletion
			var result = await _userManager.DeleteAsync(user);

			if (result.Succeeded)
			{
				TempData["Success"] = $"The account {user.Email} has been permanently removed.";
			}
			else
			{
				TempData["Error"] = "Error: Could not delete user. They might have related data in other tables.";
			}

			return RedirectToAction(nameof(Index));
		}
	}
}