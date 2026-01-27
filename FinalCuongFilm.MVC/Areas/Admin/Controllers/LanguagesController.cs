using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class LanguagesController : Controller
	{
		private readonly ILanguageService _languageService;

		public LanguagesController(ILanguageService languageService)
		{
			_languageService = languageService;
		}

		// GET: Admin/Languages
		public async Task<IActionResult> Index()
		{
			var languages = await _languageService.GetAllAsync(); // ✅ Trả về LanguageDto
			return View(languages);
		}

		// GET: Admin/Languages/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
				return NotFound();

			var language = await _languageService.GetByIdAsync(id.Value);
			if (language == null)
				return NotFound();

			return View(language);
		}

		// GET: Admin/Languages/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Admin/Languages/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(LanguageCreateDto dto)
		{
			if (ModelState.IsValid)
			{
				try
				{
					await _languageService.CreateAsync(dto);
					TempData["Success"] = "Tạo ngôn ngữ thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			return View(dto);
		}

		// GET: Admin/Languages/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null)
				return NotFound();

			var language = await _languageService.GetByIdAsync(id.Value);
			if (language == null)
				return NotFound();

			var updateDto = new LanguageUpdateDto
			{
				Id = language.Id,
				Name = language.Name
			};

			return View(updateDto);
		}

		// POST: Admin/Languages/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, LanguageUpdateDto dto)
		{
			if (id != dto.Id)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _languageService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Cập nhật ngôn ngữ thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			return View(dto);
		}

		// GET: Admin/Languages/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null)
				return NotFound();

			var language = await _languageService.GetByIdAsync(id.Value);
			if (language == null)
				return NotFound();

			return View(language);
		}

		// POST: Admin/Languages/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			try
			{
				var result = await _languageService.DeleteAsync(id);
				if (!result)
					return NotFound();

				TempData["Success"] = "Xóa ngôn ngữ thành công!";
				return RedirectToAction(nameof(Index));
			}
			catch (InvalidOperationException ex)
			{
				TempData["Error"] = ex.Message;
				return RedirectToAction(nameof(Delete), new { id });
			}
		}
	}
}