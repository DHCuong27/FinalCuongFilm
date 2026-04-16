using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UserController : Controller
	{
		private readonly UserManager<CuongFilmUser> _userManager;
		private readonly RoleManager<CuongFilmRole> _roleManager;

		// Default avatar path relative to wwwroot
		private const string DefaultAvatarUrl = "/img/avatar.jpg";

		public UserController(UserManager<CuongFilmUser> userManager, RoleManager<CuongFilmRole> roleManager)
		{
			_userManager = userManager;
			_roleManager = roleManager;
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
				// If LockoutEnd is null or in the past, the user is active
				bool isActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow;

				userDtos.Add(new UserDto
				{
					Id = user.Id,
					FullName = user.FullName ?? user.UserName,
					Email = user.Email,
					AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? DefaultAvatarUrl : user.AvatarUrl,
					Roles = await _userManager.GetRolesAsync(user),
					IsActive = isActive,
					// If you have a created date on your user model, map it here. Otherwise, map a fallback.
					// CreatedAt = user.CreatedAt 
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

		// POST: Admin/User/LockUser/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> LockUser(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			// Prevent an admin from locking themselves out
			var currentUser = await _userManager.GetUserAsync(User);
			if (currentUser != null && currentUser.Id == user.Id)
			{
				TempData["Error"] = "Action Denied: You cannot lock your own account.";
				return RedirectToAction(nameof(Index));
			}

			// Lock the account for 100 years (effectively a permanent ban)
			await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
			TempData["Success"] = $"Account for {user.Email} has been locked successfully.";

			return RedirectToAction(nameof(Index));
		}

		// POST: Admin/User/UnlockUser/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UnlockUser(string id)
		{
			if (string.IsNullOrEmpty(id)) return NotFound();

			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return NotFound();

			// Clear the lockout end date
			await _userManager.SetLockoutEndDateAsync(user, null);
			TempData["Success"] = $"Account for {user.Email} has been unlocked and restored.";

			return RedirectToAction(nameof(Index));
		}

		// GET: Admin/User/ManageRoles?userId=...
		public async Task<IActionResult> ManageRoles(string userId)
		{
			var user = await _userManager.FindByIdAsync(userId);
			if (user == null) return NotFound();

			var userRoles = await _userManager.GetRolesAsync(user);
			var allRoles = await _roleManager.Roles.ToListAsync(); // Ensure you injected RoleManager<IdentityRole>

			var model = new ManageUserRolesViewModel
			{
				UserId = userId,
				Email = user.Email,
				Roles = allRoles.Select(r => new RoleSelection
				{
					RoleName = r.Name,
					IsSelected = userRoles.Contains(r.Name)
				}).ToList()
			};

			return View(model);
		}

		// POST: Admin/User/ManageRoles
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ManageRoles(ManageUserRolesViewModel model)
		{
			var user = await _userManager.FindByIdAsync(model.UserId);
			if (user == null) return NotFound();

			var roles = await _userManager.GetRolesAsync(user);

			// Remove all current roles
			var result = await _userManager.RemoveFromRolesAsync(user, roles);
			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Failed to remove existing roles");
				return View(model);
			}

			// Add newly selected roles
			var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName);
			result = await _userManager.AddToRolesAsync(user, selectedRoles);

			if (!result.Succeeded)
			{
				ModelState.AddModelError("", "Failed to add new roles");
				return View(model);
			}

			TempData["Success"] = $"Roles updated successfully for {user.Email}";
			return RedirectToAction(nameof(Index));
		}
	}
}