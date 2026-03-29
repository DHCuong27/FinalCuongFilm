using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class UserController : Controller
	{
		private readonly UserManager<CuongFilmUser> _userManager;

		// ĐỊNH NGHĨA AVATAR MẶC ĐỊNH Ở ĐÂY (NÊN ĐỂ TRONG THƯ MỤC wwwroot)
		private const string DefaultAvatarUrl = "/img/avatar.jpg";

		public UserController(UserManager<CuongFilmUser> userManager)
		{
			_userManager = userManager;
		}

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
	}
}