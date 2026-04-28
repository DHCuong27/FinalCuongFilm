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

			var timeLimit = DateTime.UtcNow.AddMinutes(-15);

			var expiredTransactions = await _context.Transactions
				.Where(t => t.Status == TransactionStatus.Pending
						 && t.TransactionDate < timeLimit)
				.ToListAsync();

			if (expiredTransactions.Any())
			{
				foreach (var tx in expiredTransactions)
				{
					tx.Status = TransactionStatus.Failed;
				
				}

				await _context.SaveChangesAsync();
			}


			int pageSize = 10;
			var totalItems = await _context.Transactions.CountAsync();
			var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			var transactions = await _context.Transactions
				.OrderByDescending(t => t.TransactionDate)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var userIds = transactions.Select(t => t.UserId).Distinct().ToList();
			var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

			ViewBag.UserDictionary = users.ToDictionary(u => u.Id, u => u);
			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalItems = totalItems;

			return View(transactions);
		}

		public async Task<IActionResult> ActiveVips()
		{
			// ĐÃ FIX: Bỏ điều kiện "&& s.IsActive" để lấy cả những gói đang bị Khóa tạm thời
			var rawActiveVips = await _context.UserSubscriptions
				.Include(s => s.Package)
				.Where(s => s.EndDate > DateTime.UtcNow) // Chỉ lọc những gói chưa hết hạn
				.ToListAsync();

			var activeVips = rawActiveVips
				.GroupBy(s => s.UserId)
				.Select(group => group.OrderByDescending(s => s.EndDate).First())
				.ToList();

			var userIds = activeVips.Select(v => v.UserId).Distinct().ToList();
			var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();

			ViewBag.UserDictionary = users.ToDictionary(
				u => u.Id,
				u => !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Email
			);

			return View(activeVips);
		}

		// 3. Hàm Xử lý Nút Revoke (Thu hồi quyền VIP)
		[HttpPost, ActionName("Revoke")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RevokeConfirmed(Guid id)
		{
			var subscription = await _context.UserSubscriptions.FindAsync(id);
			if (subscription == null) return NotFound();

			// Tước quyền: Sửa ngày hết hạn về hiện tại và tắt cờ Active (Giữ lại lịch sử trong DB)
			subscription.EndDate = DateTime.UtcNow;
			subscription.IsActive = false;

			_context.Update(subscription);
			await _context.SaveChangesAsync();

			TempData["Success"] = "VIP access has been successfully revoked.";
			return RedirectToAction(nameof(ActiveVips));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleStatus(Guid id)
		{
			var subscription = await _context.UserSubscriptions.FindAsync(id);
			if (subscription == null) return NotFound();

			// CHỈ ĐẢO NGƯỢC CỜ IsActive (Tắt thành Mở, Mở thành Tắt). Giữ nguyên EndDate!
			subscription.IsActive = !subscription.IsActive;

			_context.Update(subscription);
			await _context.SaveChangesAsync();

			TempData["Success"] = subscription.IsActive
				? "VIP access has been successfully reactivated."
				: "VIP access has been temporarily suspended.";

			return RedirectToAction(nameof(ActiveVips));
		}

		// 4. Hàm Xử lý Nút Details (Xem chi tiết Gói)
		[HttpGet]
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
			{
				TempData["Error"] = "Invalid request: Missing ID.";
				return RedirectToAction(nameof(ActiveVips));
			}

			// Gọi chi tiết từ DB
			var vipDetails = await _context.UserSubscriptions
				.Include(s => s.Package)
				.FirstOrDefaultAsync(s => s.Id == id.Value);

			if (vipDetails == null)
			{
				TempData["Error"] = "This VIP record does not exist or has been deleted.";
				return RedirectToAction(nameof(ActiveVips));
			}

			// Truyền thông tin User sang View
			var user = await _userManager.FindByIdAsync(vipDetails.UserId);
			ViewBag.UserName = user != null ? (!string.IsNullOrEmpty(user.FullName) ? user.FullName : user.Email) : "Unknown User";
			ViewBag.UserEmail = user?.Email ?? "N/A";

			return View(vipDetails);
		}
	}
}