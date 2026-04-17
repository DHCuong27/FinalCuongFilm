using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] 
	public class VipManagerController : Controller
	{
		private readonly CuongFilmDbContext _context;
		private readonly IVipService _vipService;
		private readonly UserManager<CuongFilmUser> _userManager;

		public VipManagerController(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager, IVipService vipService)
		{
			_context = context;
			_userManager = userManager;
			_vipService = vipService;
		}

		// 1. Manage Transaction History (Payments)
		public async Task<IActionResult> Transactions(int page = 1)
		{
			int pageSize = 10;

			// 1. Get total count of records first
			var totalItems = await _context.Transactions.CountAsync();

			// 2. Calculate pagination math
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			// 3. Fetch ONLY the records for the current page
			var transactions = await _context.Transactions
				.OrderByDescending(t => t.TransactionDate)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// 4. FIX "UNKNOWN USER": Extract UserIds and fetch data from Identity DB
			var userIds = transactions.Select(t => t.UserId).Distinct().ToList();
			var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

			// Create a Dictionary mapping UserId -> CuongFilmUser object
			ViewBag.UserDictionary = users.ToDictionary(u => u.Id, u => u);

			// 5. Pass pagination metadata to the View
			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalItems = totalItems;

			return View(transactions);
		}

		// 2. Manage Active VIP Subscriptions

		public async Task<IActionResult> ActiveVips()
		{
			var activeVips = await _context.UserSubscriptions
				.Where(s => s.EndDate > DateTime.UtcNow && s.IsActive)
				.OrderByDescending(s => s.EndDate)
				.ToListAsync();

			// FIX UNKNOWN USER: Extract all unique UserIds from the subscriptions
			var userIds = activeVips.Select(v => v.UserId).Distinct().ToList();

			// Fetch user details from the Identity database
			var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

			// Create a Dictionary mapping UserId -> FullName (or Email as fallback)
			ViewBag.UserDictionary = users.ToDictionary(
				u => u.Id,
				u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Email
			);

			return View(activeVips);
		}


	}
}