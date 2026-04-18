using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class EpisodesController : Controller
	{
		private readonly IEpisodeService _episodeService;
		private readonly IMovieService _movieService;

		public EpisodesController(IEpisodeService episodeService, IMovieService movieService)
		{
			_episodeService = episodeService;
			_movieService = movieService;
		}

		// GET: Admin/Episodes
		public async Task<IActionResult> Index(Guid? movieId, int page = 1)
		{
			int pageSize = 10;

			// FIX: Pass movieId explicitly to filter by Series, rather than using it as a text search string
			var result = await _episodeService.GetPagedAsync(movieId, page, pageSize);

			if (movieId.HasValue)
			{
				var movie = await _movieService.GetByIdAsync(movieId.Value);
				ViewBag.MovieTitle = movie?.Title;
				ViewBag.MovieId = movieId.Value;
			}

			return View(result);
		}

		// GET: Admin/Episodes/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null) return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null) return NotFound();

			return View(episode);
		}

		// GET: Admin/Episodes/Create
		public async Task<IActionResult> Create(Guid? movieId)
		{
			if (movieId.HasValue)
			{
				var movie = await _movieService.GetByIdAsync(movieId.Value);
				if (movie != null && movie.Type == MovieType.Movie)
				{
					TempData["Error"] = "Error: You cannot add episodes to a standalone Movie. Only Series allow episodes.";
					return RedirectToAction("Details", "Movies", new { id = movieId.Value });
				}
			}

			await PopulateMoviesDropdown(movieId);

			var model = new EpisodeCreateDto();
			if (movieId.HasValue)
			{
				model.MovieId = movieId.Value;
			}

			return View(model);
		}

		// POST: Admin/Episodes/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(EpisodeCreateDto dto)
		{
			var movie = await _movieService.GetByIdAsync(dto.MovieId);
			if (movie != null && movie.Type == MovieType.Movie)
			{
				ModelState.AddModelError("", "Cannot add episodes to a standalone Movie.");
			}

			if (ModelState.IsValid)
			{
				try
				{
					await _episodeService.CreateAsync(dto);
					TempData["Success"] = "Episode created successfully!";
					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"System Error: {ex.Message}");
				}
			}

			await PopulateMoviesDropdown(dto.MovieId);
			return View(dto);
		}

		// GET: Admin/Episodes/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null) return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null) return NotFound();

			var updateDto = new EpisodeUpdateDto
			{
				Id = episode.Id,
				EpisodeNumber = episode.EpisodeNumber,
				Title = episode.Title,
				Description = episode.Description,
				DurationMinutes = episode.DurationMinutes,
				AirDate = episode.AirDate,
				IsActive = episode.IsActive,
				MovieId = episode.MovieId
			};

			await PopulateMoviesDropdown(episode.MovieId);
			return View(updateDto);
		}

		// POST: Admin/Episodes/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, EpisodeUpdateDto dto)
		{
			if (id != dto.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _episodeService.UpdateAsync(dto);
					if (!result) return NotFound();

					TempData["Success"] = "Episode updated successfully!";
					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"System Error: {ex.Message}");
				}
			}

			await PopulateMoviesDropdown(dto.MovieId);
			return View(dto);
		}

		// GET: Admin/Episodes/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null) return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null) return NotFound();

			return View(episode);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			// 1. Tìm tập phim chuẩn bị xóa
			var episode = await _episodeService.GetByIdAsync(id);
			if (episode == null) return NotFound();

			// 2. [QUAN TRỌNG NHẤT]: Lưu lại ID của bộ phim TRƯỚC KHI xóa tập phim đó đi
			Guid currentMovieId = episode.MovieId;

			try
			{
				// 3. Tiến hành xóa tập phim
				await _episodeService.DeleteAsync(id);
				TempData["Success"] = "Deleted episode successfully!";
			}
			catch (Exception ex)
			{
				TempData["Error"] = "Error deleting episode: " + ex.Message;
				return RedirectToAction(nameof(Delete), new { id = id });
			}

			// 4. [FIX LỖI]: Điều hướng về trang Index VÀ bắt buộc truyền kèm tham số movieId
			return RedirectToAction(nameof(Index), new { movieId = currentMovieId });
		}

		private async Task PopulateMoviesDropdown(Guid? selectedId = null)
		{
			var allMovies = await _movieService.GetAllAsync();

			// STRICT BUSINESS LOGIC: Filter only movies that are defined as a Series
			var seriesOnly = allMovies.Where(m => m.Type == MovieType.Series).ToList();

			// FIXED: Must use ViewBag.Movies to match the asp-items in the HTML Views
			ViewBag.Movies = new SelectList(seriesOnly, "Id", "Title", selectedId);
		}
	}
}