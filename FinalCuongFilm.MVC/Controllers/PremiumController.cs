using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class PremiumController : Controller
	{
		private readonly IVipService _vipService;

		public PremiumController(IVipService vipService)
		{
			_vipService = vipService;
		}

		// Trang hiển thị Bảng giá
		public async Task<IActionResult> Index()
		{
			var packages = await _vipService.GetActivePackagesAsync();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId != null)
			{
				var currentVip = await _vipService.GetCurrentUserSubscriptionAsync(userId);
				ViewBag.CurrentVipEndDate = currentVip?.EndDate;
			}

			return View(packages);
		}

		// Nút Mua ngay gọi vào đây
		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Checkout(Guid packageId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null) return RedirectToAction("Login", "Auth");

			// 1. Vẫn tạo giao dịch Pending bình thường
			var transaction = await _vipService.CreateTransactionAsync(userId, packageId);

			// ==========================================
			// DEV HACK: GIẢ LẬP THANH TOÁN THÀNH CÔNG VÀ CHẠY LUÔN HÀM CỘNG VIP
			// Bỏ qua bước gọi URL VNPay
			// ==========================================

			bool isSuccess = await _vipService.CompleteTransactionAsync(transaction.Id, "00");

			if (isSuccess)
			{
				TempData["Success"] = "[TEST MODE] Đã giả lập thanh toán thành công và cộng ngày VIP!";
			}
			else
			{
				TempData["Error"] = "Lỗi khi giả lập thanh toán.";
			}

			return RedirectToAction("Index"); // Quay lại trang bảng giá để xem hạn VIP mới
		}
	}
}