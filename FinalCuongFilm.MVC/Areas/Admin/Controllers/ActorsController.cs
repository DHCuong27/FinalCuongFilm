using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class ActorsController : Controller
	{
		private readonly IActorService _actorService;


		public ActorsController(IActorService actorService)
		{
			_actorService = actorService;
		}

		// GET: Admin/Actors
		public async Task<IActionResult> Index(int page = 1)
		{
			int pageSize = 20; 

			// Gọi hàm phân trang vừa viết
			var pagedData = await _actorService.GetPagedAsync(page, pageSize);

			return View(pagedData); // Đẩy PagedResult ra View
		}

		// GET: Admin/Actors/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
				return NotFound();

			var actor = await _actorService.GetByIdAsync(id.Value);
			if (actor == null)
				return NotFound();

			return View(actor);
		}

		// GET: Admin/Actors/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Admin/Actors/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ActorCreateDto dto)
		{
			if (ModelState.IsValid)
			{
				try
				{
					await _actorService.CreateAsync(dto);
					TempData["Success"] = "Tạo diễn viên thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			return View(dto);
		}

		// GET: Admin/Actors/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null)
				return NotFound();

			var actor = await _actorService.GetByIdAsync(id.Value);
			if (actor == null)
				return NotFound();

			var updateDto = new ActorUpdateDto
			{
				Id = actor.Id,
				Name = actor.Name,
				AvartUrl = actor.AvartUrl,
				DateOfBirth = actor.DateOfBirth,
				Gender = actor.Gender
			};

			return View(updateDto);
		}

		// POST: Admin/Actors/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, ActorUpdateDto dto)
		{
			if (id != dto.Id)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _actorService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Cập nhật diễn viên thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			return View(dto);
		}

		// GET: Admin/Actors/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null)
				return NotFound();

			var actor = await _actorService.GetByIdAsync(id.Value);
			if (actor == null)
				return NotFound();

			return View(actor);
		}

		// POST: Admin/Actors/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			try
			{
				var result = await _actorService.DeleteAsync(id);
				if (!result)
					return NotFound();

				TempData["Success"] = "Xóa diễn viên thành công!";
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