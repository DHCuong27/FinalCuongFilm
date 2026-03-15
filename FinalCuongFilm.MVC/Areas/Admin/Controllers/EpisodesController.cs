using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
		public async Task<IActionResult> Index(Guid? movieId)
		{
			IEnumerable<EpisodeDto> episodes;

			if (movieId.HasValue)
			{
				episodes = await _episodeService.GetByMovieIdAsync(movieId.Value);
				var movie = await _movieService.GetByIdAsync(movieId.Value);
				ViewBag.MovieTitle = movie?.Title;
				ViewBag.MovieId = movieId.Value;
			}
			else
			{
				episodes = await _episodeService.GetAllAsync();
			}

			return View(episodes);
		}

		// GET: Admin/Episodes/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
				return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null)
				return NotFound();

			return View(episode);
		}

		// GET: Admin/Episodes/Create
		public async Task<IActionResult> Create(Guid? movieId)
		{
			if (movieId.HasValue)
			{
				// FIX: Check if the movie is actually a Series
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
			// FIX: Double-check validation on POST to prevent bypass
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
					TempData["Success"] = "Tạo tập phim thành công!";
					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateMoviesDropdown(dto.MovieId);
			return View(dto);
		}

		// GET: Admin/Episodes/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null)
				return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null)
				return NotFound();

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
			if (id != dto.Id)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _episodeService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Cập nhật tập phim thành công!";
					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateMoviesDropdown(dto.MovieId);
			return View(dto);
		}

		// GET: Admin/Episodes/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null)
				return NotFound();

			var episode = await _episodeService.GetByIdAsync(id.Value);
			if (episode == null)
				return NotFound();

			return View(episode);
		}

		// POST: Admin/Episodes/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			var episode = await _episodeService.GetByIdAsync(id);
			if (episode == null) return NotFound();

			try
			{
				await _episodeService.DeleteAsync(id);
				TempData["Success"] = "Xóa tập phim thành công!";
				return RedirectToAction(nameof(Index), new { movieId = episode.MovieId });
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Lỗi: {ex.Message}";
				return RedirectToAction(nameof(Delete), new { id });
			}
		}

		private async Task PopulateMoviesDropdown(Guid? selectedId = null)
		{
			var allMovies = await _movieService.GetAllAsync();
			var seriesOnly = allMovies.Where(m => m.Type == MovieType.Series);

			// Đổi "MovieId" thành "Movies" để khớp với View
			ViewBag.Movies = new SelectList(seriesOnly, "Id", "Title", selectedId);
		}
	}
}