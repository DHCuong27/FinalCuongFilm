using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

		public VipManagerController(CuongFilmDbContext context, IVipService vipService)
		{
			_context = context;
			_vipService = vipService;
		}

		// 1. Manage Transaction History (Payments)
		public async Task<IActionResult> Transactions(int page = 1)
		{
			int pageSize = 10; 

			// Get total count of records first
			var totalItems = await _context.Transactions.CountAsync();

			// Calculate pagination math
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			// Fetch ONLY the records for the current page using Skip and Take
			var transactions = await _context.Transactions
				.OrderByDescending(t => t.TransactionDate)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			// Pass pagination metadata to the View via ViewBag
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

			return View(activeVips);
		}

	
	}
}