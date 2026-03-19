using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer; // Lưu ý: Dùng 1 using DataLayer chuẩn thôi nhé
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class MoviesController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IActorService _actorService;
		private readonly IGenreService _genreService;
		private readonly CuongFilmDbContext _context;
		private readonly IAzureBlobService _azureBlobService;

		// Thêm Service Import vào đây
		private readonly IMovieImportService _movieImportService;

		public MoviesController(
			IMovieService movieService,
			IActorService actorService,
			IGenreService genreService,
			CuongFilmDbContext context,
			IAzureBlobService azureBlobService,
			IMovieImportService movieImportService) // Inject vào
		{
			_movieService = movieService;
			_actorService = actorService;
			_genreService = genreService;
			_context = context;
			_azureBlobService = azureBlobService;
			_movieImportService = movieImportService; // Gán biến
		}

		// GET: Admin/Movies
		public async Task<IActionResult> Index(int page = 1)
		{
			int pageSize = 10; // Đặt số lượng phim hiển thị trên 1 trang (có thể đổi thành 5 hoặc 20)

			// Gọi hàm phân trang vừa viết
			var pagedData = await _movieService.GetPagedAsync(page, pageSize);

			return View(pagedData); // Đẩy PagedResult ra View
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ImportFromTmdb([FromForm] string title)
		{
			if (string.IsNullOrWhiteSpace(title))
				return Json(new { success = false, message = "Please input movie name" });

			try
			{
				// Lấy kết quả từ Service
				var result = await _movieImportService.ImportMovieAsync(title);

				// Trả về đúng trạng thái và thông điệp thực tế
				return Json(new { success = result.Success, message = result.Message });
			}
			catch (Exception ex)
			{
				var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
				return Json(new { success = false, message = $"Error system: {innerMessage}" });
			}
		}

		// GET: Admin/Movies/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
				return NotFound();

			var movie = await _movieService.GetByIdAsync(id.Value);
			if (movie == null)
				return NotFound();

			return View(movie);

		}

		// GET: Admin/Movies/Create
		public async Task<IActionResult> Create()
		{
			await PopulateDropdowns();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MovieCreateDto dto, IFormFile? posterFile)
		{
			if (posterFile != null && posterFile.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				var ext = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(ext))
					ModelState.AddModelError("", "Poster file must be JPG, PNG, or WEBP.");
				else if (posterFile.Length > 5 * 1024 * 1024)
					ModelState.AddModelError("", "Poster file must be smaller than 5MB.");
			}

			if (ModelState.IsValid)
			{
				try
				{
					if (posterFile != null && posterFile.Length > 0)
					{
						var tempSlug = !string.IsNullOrEmpty(dto.Slug)
							? dto.Slug
							: dto.Title.ToLower().Replace(" ", "-");
						dto.PosterUrl = await _azureBlobService.UploadPosterAsync(posterFile, tempSlug);
					}

					await _movieService.CreateAsync(dto);
					TempData["Success"] = "Add Film Successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (DbUpdateException dbEx)
				{
					
					var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
					ModelState.AddModelError("", $"Error database: {innerException}");

					// Log to console
					Console.WriteLine($"[ERROR] DbUpdateException: {innerException}");
					Console.WriteLine($"[STACK] {dbEx.StackTrace}");
				}
				catch (Exception ex)
				{
					//  LOG LỖI CHUNG
					var innerException = ex.InnerException?.Message ?? ex.Message;
					ModelState.AddModelError("", $"Lỗi: {innerException}");

					Console.WriteLine($"[ERROR] Exception: {innerException}");
					Console.WriteLine($"[STACK] {ex.StackTrace}");
				}
			}

			// Reload dropdowns
			await PopulateDropdowns();
			return View(dto);
		}

		// GET: Admin/Movies/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null)
				return NotFound();

			var movie = await _movieService.GetByIdAsync(id.Value);
			if (movie == null)
				return NotFound();

			var updateDto = new MovieUpdateDto
			{
				Id = movie.Id,
				Title = movie.Title,
				Description = movie.Description,
				ReleaseYear = movie.ReleaseYear,
				DurationMinutes = movie.DurationMinutes,
				PosterUrl = movie.PosterUrl,
				TrailerUrl = movie.TrailerUrl,
				Type = movie.Type,
				Status = movie.Status,
				IsActive = movie.IsActive,
				LanguageId = movie.LanguageId,
				CountryId = movie.CountryId,
				ActorIds = movie.SelectedActorIds,
				GenreIds = movie.SelectedGenreIds
			};

			await PopulateDropdowns(movie.LanguageId, movie.CountryId, movie.SelectedActorIds, movie.SelectedGenreIds);
			return View(updateDto);
		}

		// POST: Admin/Movies/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, MovieUpdateDto dto, IFormFile? posterFile)
		{
			if (id != dto.Id)
				return NotFound();

			if (posterFile != null && posterFile.Length > 0)
			{
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
				var ext = Path.GetExtension(posterFile.FileName).ToLowerInvariant();
				if (!allowedExtensions.Contains(ext))
					ModelState.AddModelError("", "Poster file must be JPG, PNG, or WEBP.");
				else if (posterFile.Length > 5 * 1024 * 1024)
					ModelState.AddModelError("", "Poster file must be smaller than 5MB.");
			}

			if (ModelState.IsValid)
			{
				try
				{
					if (posterFile != null && posterFile.Length > 0)
					{
						var slug = !string.IsNullOrEmpty(dto.Slug) ? dto.Slug : dto.Id.ToString();
						dto.PosterUrl = await _azureBlobService.UploadPosterAsync(posterFile, slug);
					}

					var result = await _movieService.UpdateAsync(id, dto);
					if (result == null)
						return NotFound();

					TempData["Success"] = "Update film successfully!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Fail to update: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.LanguageId, dto.CountryId, dto.ActorIds, dto.GenreIds);
			return View(dto);
		}

		// GET: Admin/Movies/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null)
				return NotFound();

			var movie = await _movieService.GetByIdAsync(id.Value);
			if (movie == null)
				return NotFound();

			return View(movie);
		}

		// POST: Admin/Movies/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			try
			{
				var result = await _movieService.DeleteAsync(id);
				if (!result)
					return NotFound();

				TempData["Success"] = "Delete film succesfully!";
				return RedirectToAction(nameof(Index));
			}
			catch (InvalidOperationException ex)
			{
				TempData["Error"] = ex.Message;
				return RedirectToAction(nameof(Delete), new { id });
			}
		}

		private async Task PopulateDropdowns(
			Guid? selectedLanguageId = null,
			Guid? selectedCountryId = null,
			List<Guid>? selectedActorIds = null,
			List<Guid>? selectedGenreIds = null)
		{
			var actors = await _actorService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();

			ViewBag.Actors = new MultiSelectList(actors, "Id", "Name", selectedActorIds);
			ViewBag.Genres = new MultiSelectList(genres, "Id", "Name", selectedGenreIds);
			ViewBag.Languages = new SelectList(_context.Languages, "Id", "Name", selectedLanguageId);
			ViewBag.Countries = new SelectList(_context.Countries, "Id", "Name", selectedCountryId);
		}
	}
}