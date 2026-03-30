using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class ActorsController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IActorService _actorService;
		private readonly IAzureBlobService _azureBlobService;
		private readonly CuongFilmDbContext _context;

		public ActorsController(
			IMovieService movieService,
			IActorService actorService,
			CuongFilmDbContext context,
			IAzureBlobService azureBlobService
		)
		{
			_movieService = movieService;
			_actorService = actorService;
			_azureBlobService = azureBlobService;
			_context = context;
		}

		// GET: Admin/Actors
		public async Task<IActionResult> Index(string? searchString, int page = 1)
		{
			int pageSize = 10;

			ViewBag.CurrentSearch = searchString;

			var pagedData = await _actorService.GetPagedAsync(searchString, page, pageSize);

			return View(pagedData);
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
		public async Task<IActionResult> Create()
		{
			// Get the list of movies to populate the Tag Picker
			await PopulateMoviesDropdown();
			return View();
		}

		// POST: Admin/Actors/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(ActorCreateDto dto, IFormFile? posterFile)
		{
			// Validate image file (Same as Movie)
			if (posterFile != null && posterFile.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				var ext = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(ext))
					ModelState.AddModelError("", "Avatar file must be JPG, PNG, or WEBP.");
				else if (posterFile.Length > 5 * 1024 * 1024)
					ModelState.AddModelError("", "Avatar file must be smaller than 5MB.");
			}

			if (ModelState.IsValid)
			{
				try
				{
					// 1. Upload image to Azure if available
					if (posterFile != null && posterFile.Length > 0)
					{
						var tempSlug = dto.Name?.ToLower().Replace(" ", "-") ?? Guid.NewGuid().ToString();
						dto.AvartUrl = await _azureBlobService.UploadPosterAsync(posterFile, tempSlug);
					}

					// 2. Call Service to handle Actor creation + Assign MovieIds
					await _actorService.CreateAsync(dto);

					TempData["Success"] = "Actor created successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"System error: {ex.Message}");
				}
			}

			await PopulateMoviesDropdown();
			return View(dto);
		}

		private async Task PopulateMoviesDropdown()
		{
			// Business logic: Only fetch Active movies, and only get Id + Title to optimize RAM
			var movies = await _context.Movies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ReleaseYear) // Prioritize new movies at the top
				.Select(m => new { m.Id, m.Title })
				.ToListAsync();

			// Cast to SelectList so the HTML <select> tag can understand it
			ViewBag.Movies = new SelectList(movies, "Id", "Title");
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
				Gender = actor.Gender,
				MovieIds = actor.SelectedMovieIds
			};

			await PopulateMoviesDropdown();
			return View(updateDto);
		}

		// POST: Admin/Actors/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		// FIX: Added IFormFile? posterFile to support Avatar updates during Edit
		public async Task<IActionResult> Edit(Guid id, ActorUpdateDto dto, IFormFile? posterFile)
		{
			if (id != dto.Id)
				return NotFound();

			// Validate image file
			if (posterFile != null && posterFile.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				var ext = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(ext))
					ModelState.AddModelError("", "Avatar file must be JPG, PNG, or WEBP.");
				else if (posterFile.Length > 5 * 1024 * 1024)
					ModelState.AddModelError("", "Avatar file must be smaller than 5MB.");
			}

			if (ModelState.IsValid)
			{
				try
				{
					// Upload new image to Azure if available
					if (posterFile != null && posterFile.Length > 0)
					{
						var tempSlug = dto.Name?.ToLower().Replace(" ", "-") ?? dto.Id.ToString();
						dto.AvartUrl = await _azureBlobService.UploadPosterAsync(posterFile, tempSlug);
					}

					var result = await _actorService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Actor updated successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Error: {ex.Message}");
				}
			}

			// FIX: Must reload the dropdown data if ModelState is invalid
			await PopulateMoviesDropdown();
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

				TempData["Success"] = "Actor deleted successfully!";
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