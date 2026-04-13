using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] // Only Admin can access
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
			int pageSize = 10; // Number of items per page

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

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ManualConfirm(Guid id)
		{
			try
			{
				// 1. FETCH TRANSACTION TO VERIFY STATE
				var transaction = await _context.Transactions.FindAsync(id);
				if (transaction == null) return NotFound();

				// SAFEGUARD: Prevent confirming an already successful transaction
				if (transaction.Status == FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Success)
				{
					TempData["Error"] = "Warning: This transaction has already been marked as SUCCESS automatically. No need to confirm again!";
					return RedirectToAction("Transactions");
				}

				// SAFEGUARD: Prevent confirming a cancelled transaction
				if (transaction.Status == FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Failed)
				{
					TempData["Error"] = "Error: This transaction has been CANCELLED. You cannot revive a cancelled transaction. The customer must create a new order.";
					return RedirectToAction("Transactions");
				}

				// 2. IF SAFE (Still Pending), PROCEED WITH CONFIRMATION
				await _vipService.CompleteTransactionAsync(id, true);
				TempData["Success"] = "Successfully confirmed! VIP privileges have been activated for the customer.";
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"System error: {ex.Message}";
			}

			return RedirectToAction("Transactions");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ManualCancel(Guid id)
		{
			try
			{
				// 1. FETCH TRANSACTION TO VERIFY STATE
				var transaction = await _context.Transactions.FindAsync(id);
				if (transaction == null) return NotFound();

				// ULTIMATE SAFEGUARD: NEVER ALLOW CANCELLING A SUCCESSFUL TRANSACTION
				if (transaction.Status == FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Success)
				{
					TempData["Error"] = "⛔ STOP: The customer has successfully paid and received VIP. YOU ARE NOT ALLOWED TO CANCEL THIS TRANSACTION!";
					return RedirectToAction("Transactions");
				}

				// 2. PREVENT PREMATURE CANCELLATION (15-Minute Rule)
				// Only allow cancelling transactions older than 15 minutes to give customers time to scan the QR code.
				var timeElapsed = DateTime.UtcNow - transaction.TransactionDate;

				// FIX: Used TotalMinutes instead of Minutes to ensure accurate calculation
				if (timeElapsed.TotalMinutes < 15)
				{
					TempData["Error"] = $"This transaction was created only {(int)timeElapsed.TotalMinutes} minutes ago. Please wait 15 minutes to give the customer time to pay before cancelling.";
					return RedirectToAction("Transactions");
				}

				// 3. IF CONDITIONS MET, PROCEED WITH CANCELLATION
				if (transaction.Status == FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Pending)
				{
					await _vipService.CompleteTransactionAsync(id, false);
					TempData["Success"] = "Transaction has been safely cancelled and cleaned up.";
				}
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"System error: {ex.Message}";
			}

			return RedirectToAction("Transactions");
		}
	}
}