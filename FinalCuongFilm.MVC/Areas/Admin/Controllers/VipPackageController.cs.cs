using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] // Bắt buộc phải là Admin
	public class VipPackageController : Controller
	{
		private readonly CuongFilmDbContext _context;

		public VipPackageController(CuongFilmDbContext context)
		{
			_context = context;
		}

		// 1. DANH SÁCH GÓI VIP
		public async Task<IActionResult> Index()
		{
			var packages = await _context.VipPackages.OrderBy(p => p.Price).ToListAsync();
			return View(packages);
		}

		// 2. THÊM GÓI (GET)
		public IActionResult Create()
		{
			return View(new VipPackage { IsActive = true }); // Mặc định tạo ra là được Active
		}

		// 3. THÊM GÓI (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(VipPackage model)
		{
			if (ModelState.IsValid)
			{
				model.Id = Guid.NewGuid();
				_context.VipPackages.Add(model);
				await _context.SaveChangesAsync();
				TempData["Success"] = "Thêm gói VIP thành công!";
				return RedirectToAction(nameof(Index));
			}
			return View(model);
		}

		// 4. SỬA GÓI (GET)
		public async Task<IActionResult> Edit(Guid id)
		{
			var package = await _context.VipPackages.FindAsync(id);
			if (package == null) return NotFound();
			return View(package);
		}

		// 5. SỬA GÓI (POST)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, VipPackage model)
		{
			if (id != model.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(model);
					await _context.SaveChangesAsync();
					TempData["Success"] = "Cập nhật gói VIP thành công!";
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!await _context.VipPackages.AnyAsync(e => e.Id == id)) return NotFound();
					else throw;
				}
				return RedirectToAction(nameof(Index));
			}
			return View(model);
		}
	}
}