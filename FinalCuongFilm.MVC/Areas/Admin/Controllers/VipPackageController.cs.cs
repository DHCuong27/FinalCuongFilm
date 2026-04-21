using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] // Bắt buộc phải là Admin mới vào được
	public class VipPackageController : Controller
	{
		private readonly IVipService _vipService;
		private readonly CuongFilmDbContext _context;

		public VipPackageController(IVipService vipService, CuongFilmDbContext context)
		{
			_vipService = vipService;
			_context = context;
		}

		// 1. READ (Hiển thị danh sách)
		public async Task<IActionResult> Index()
		{
			var packages = await _vipService.GetAllPackagesAsync();
			return View(packages);
		}

		// 2. CREATE (GET)
		[HttpGet]
		public IActionResult Create()
		{
			return View(new VipPackage { IsActive = true });
		}

		// 2. CREATE (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(VipPackage model)
		{
			if (ModelState.IsValid)
			{
				await _vipService.CreatePackageAsync(model);
				TempData["Success"] = "VIP package created successfully!";
				return RedirectToAction(nameof(Index));
			}
			return View(model);
		}

		// 3. UPDATE (GET)
		[HttpGet]
		public async Task<IActionResult> Edit(Guid id)
		{
			var package = await _vipService.GetPackageByIdAsync(id);
			if (package == null) return NotFound();

			return View(package);
		}

		// 3. UPDATE (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, VipPackage model)
		{
			if (id != model.Id) return BadRequest();

			if (ModelState.IsValid)
			{
				await _vipService.UpdatePackageAsync(model);
				TempData["Success"] = "VIP package updated successfully!";
				return RedirectToAction(nameof(Index));
			}
			return View(model);
		}

		// 4. DELETE (Soft Delete - POST từ một form hoặc nút bấm)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _vipService.DeactivatePackageAsync(id);
			TempData["Success"] = "VIP package deactivated successfully!";
			return RedirectToAction(nameof(Index));
		}

		// 2. BẮT BUỘC PHẢI CÓ HÀM NÀY VÀ GẮN [HttpPost]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ToggleStatus(Guid id)
		{
			var package = await _context.VipPackages.FindAsync(id);
			if (package == null)
			{
				TempData["Error"] = "Package not found!";
				return RedirectToAction(nameof(Index));
			}

			// Lật ngược trạng thái: Nếu đang True (Bán) -> Thành False (Ẩn), và ngược lại
			package.IsActive = !package.IsActive;

			_context.Update(package);
			await _context.SaveChangesAsync();

			TempData["Success"] = package.IsActive
				? $"Package is back on sale: {package.Name}"
				: $"Package is no longer on sale: {package.Name}";

			return RedirectToAction(nameof(Index));
		}
	}
}