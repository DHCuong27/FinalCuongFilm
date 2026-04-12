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

		// GET: /Premium/Index
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

		// GET: /Premium/Checkout
		// Displays the confirmation/invoice page before paying
		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Checkout(Guid packageId)
		{
			var packages = await _vipService.GetActivePackagesAsync();
			var selectedPackage = packages.FirstOrDefault(p => p.Id == packageId);

			if (selectedPackage == null)
			{
				TempData["Error"] = "This package does not exist.";
				return RedirectToAction("Index");
			}

			return View(selectedPackage);
		}

		// POST: /Premium/ProcessPayment
		// Processes the actual (mock) payment
		[Authorize]
		[HttpPost]
		public async Task<IActionResult> ProcessPayment(Guid packageId)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (userId == null) return RedirectToAction("Login", "Auth");

				// 1. Create Pending transaction in DB
				var transaction = await _vipService.CreateTransactionAsync(userId, packageId);

				// =========================================================
				// 🚨 DEV HACK: MOCK PAYMENT 
				// =========================================================
				bool isSuccess = await _vipService.CompleteTransactionAsync(transaction.Id, "00");

				if (isSuccess)
					TempData["Success"] = "[TEST MODE] Mock payment successful! VIP days have been added.";
				else
					TempData["Error"] = "Error during mock payment.";

				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				TempData["Error"] = "An error occurred: " + ex.Message;
				return RedirectToAction("Index");
			}
		}
	}
}