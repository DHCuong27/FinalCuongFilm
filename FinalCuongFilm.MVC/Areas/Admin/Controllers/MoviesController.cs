using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

		public MoviesController(
			IMovieService movieService,
			IActorService actorService,
			IGenreService genreService,
			CuongFilmDbContext context)
		{
			_movieService = movieService;
			_actorService = actorService;
			_genreService = genreService;
			_context = context;
		}

		// GET: Admin/Movies
		public async Task<IActionResult> Index()
		{
			var movies = await _movieService.GetAllAsync();
			return View(movies);
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

		// POST: Admin/Movies/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MovieCreateDto dto)
		{
			if (ModelState.IsValid)
			{
				try
				{
					await _movieService.CreateAsync(dto);
					TempData["Success"] = "Tạo phim thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi khi tạo phim: {ex.Message}");
				}
			}

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
		public async Task<IActionResult> Edit(Guid id, MovieUpdateDto dto)
		{
			if (id != dto.Id)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _movieService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Cập nhật phim thành công!";
					return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi khi cập nhật: {ex.Message}");
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

				TempData["Success"] = "Xóa phim thành công!";
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