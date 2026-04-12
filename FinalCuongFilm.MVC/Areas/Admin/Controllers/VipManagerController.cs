using FinalCuongFilm.DataLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] // Chỉ Admin mới được vào
	public class VipManagerController : Controller
	{
		private readonly CuongFilmDbContext _context;

		public VipManagerController(CuongFilmDbContext context)
		{
			_context = context;
		}

		// 1. Quản lý Lịch sử giao dịch (Payments)
		public async Task<IActionResult> Transactions()
		{
			// Lấy danh sách giao dịch, join với bảng VipPackage để lấy tên gói
			var transactions = await _context.Transactions
				.OrderByDescending(t => t.TransactionDate)
				.ToListAsync();

			// Để lấy được Username, bạn có thể Join thêm với UserManager (Identity) 
			// hoặc truyền raw UserId ra View rồi xử lý.

			return View(transactions);
		}

		// 2. Quản lý Tài khoản VIP đang Active
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