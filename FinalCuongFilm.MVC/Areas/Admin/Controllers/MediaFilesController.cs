using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class MediaFilesController : Controller
	{
		private readonly IMediaFileService _mediaFileService;
		private readonly IMovieService _movieService;
		private readonly IEpisodeService _episodeService;

		public MediaFilesController(
			IMediaFileService mediaFileService,
			IMovieService movieService,
			IEpisodeService episodeService)
		{
			_mediaFileService = mediaFileService;
			_movieService = movieService;
			_episodeService = episodeService;
		}

		// GET: Admin/MediaFiles
		public async Task<IActionResult> Index(Guid? movieId, Guid? episodeId)
		{
			IEnumerable<MediaFileDto> mediaFiles;

			if (episodeId.HasValue)
			{
				mediaFiles = await _mediaFileService.GetByEpisodeIdAsync(episodeId.Value);
				var episode = await _episodeService.GetByIdAsync(episodeId.Value);
				ViewBag.EpisodeTitle = $"Tập {episode?.EpisodeNumber}: {episode?.Title}";
				ViewBag.EpisodeId = episodeId.Value;
				ViewBag.MovieId = episode?.MovieId;
			}
			else if (movieId.HasValue)
			{
				mediaFiles = await _mediaFileService.GetByMovieIdAsync(movieId.Value);
				var movie = await _movieService.GetByIdAsync(movieId.Value);
				ViewBag.MovieTitle = movie?.Title;
				ViewBag.MovieId = movieId.Value;
			}
			else
			{
				mediaFiles = await _mediaFileService.GetAllAsync();
			}

			return View(mediaFiles);
		}

		// GET: Admin/MediaFiles/Details/5
		public async Task<IActionResult> Details(Guid? id)
		{
			if (id == null)
				return NotFound();

			var mediaFile = await _mediaFileService.GetByIdAsync(id.Value);
			if (mediaFile == null)
				return NotFound();

			return View(mediaFile);
		}

		// GET: Admin/MediaFiles/Create
		public async Task<IActionResult> Create(Guid? movieId, Guid? episodeId)
		{
			await PopulateDropdowns(movieId, episodeId);

			var model = new MediaFileCreateDto
			{
				MovieId = movieId,
				EpisodeId = episodeId
			};

			return View(model);
		}

		// POST: Admin/MediaFiles/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(MediaFileCreateDto dto)
		{
			if (ModelState.IsValid)
			{
				try
				{
					await _mediaFileService.CreateAsync(dto);
					TempData["Success"] = "Tạo media file thành công!";

					if (dto.EpisodeId.HasValue)
						return RedirectToAction(nameof(Index), new { episodeId = dto.EpisodeId });
					else if (dto.MovieId.HasValue)
						return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
					else
						return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.MovieId, dto.EpisodeId);
			return View(dto);
		}

		// GET: Admin/MediaFiles/Upload
		public async Task<IActionResult> Upload(Guid? movieId, Guid? episodeId)
		{
			await PopulateDropdowns(movieId, episodeId);

			var model = new MediaFileUploadDto
			{
				MovieId = movieId,
				EpisodeId = episodeId
			};

			return View(model);
		}

		// POST: Admin/MediaFiles/Upload
		[HttpPost]
		[ValidateAntiForgeryToken]
		[RequestSizeLimit(2147483648)] // 2GB limit
		public async Task<IActionResult> Upload(MediaFileUploadDto dto)
		{
			if (ModelState.IsValid)
			{
				try
				{
					await _mediaFileService.UploadAsync(dto);
					TempData["Success"] = "Upload file thành công!";

					if (dto.EpisodeId.HasValue)
						return RedirectToAction(nameof(Index), new { episodeId = dto.EpisodeId });
					else if (dto.MovieId.HasValue)
						return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
					else
						return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.MovieId, dto.EpisodeId);
			return View(dto);
		}

		// GET: Admin/MediaFiles/Edit/5
		public async Task<IActionResult> Edit(Guid? id)
		{
			if (id == null)
				return NotFound();

			var mediaFile = await _mediaFileService.GetByIdAsync(id.Value);
			if (mediaFile == null)
				return NotFound();

			var updateDto = new MediaFileUpdateDto
			{
				Id = mediaFile.Id,
				FileName = mediaFile.FileName,
				FileUrl = mediaFile.FileUrl,
				FilePath = mediaFile.FilePath,
				FileSizeBytes = mediaFile.FileSizeBytes,
				FileType = mediaFile.FileType,
				Quality = mediaFile.Quality,
				Language = mediaFile.Language,
				MovieId = mediaFile.MovieId,
				EpisodeId = mediaFile.EpisodeId
			};

			await PopulateDropdowns(mediaFile.MovieId, mediaFile.EpisodeId);
			return View(updateDto);
		}

		// POST: Admin/MediaFiles/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, MediaFileUpdateDto dto)
		{
			if (id != dto.Id)
				return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					var result = await _mediaFileService.UpdateAsync(dto);
					if (!result)
						return NotFound();

					TempData["Success"] = "Cập nhật media file thành công!";

					if (dto.EpisodeId.HasValue)
						return RedirectToAction(nameof(Index), new { episodeId = dto.EpisodeId });
					else if (dto.MovieId.HasValue)
						return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
					else
						return RedirectToAction(nameof(Index));
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.MovieId, dto.EpisodeId);
			return View(dto);
		}

		// GET: Admin/MediaFiles/Delete/5
		public async Task<IActionResult> Delete(Guid? id)
		{
			if (id == null)
				return NotFound();

			var mediaFile = await _mediaFileService.GetByIdAsync(id.Value);
			if (mediaFile == null)
				return NotFound();

			return View(mediaFile);
		}

		// POST: Admin/MediaFiles/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			try
			{
				var mediaFile = await _mediaFileService.GetByIdAsync(id);
				var episodeId = mediaFile?.EpisodeId;
				var movieId = mediaFile?.MovieId;

				var result = await _mediaFileService.DeleteAsync(id);
				if (!result)
					return NotFound();

				TempData["Success"] = "Xóa media file thành công!";

				if (episodeId.HasValue)
					return RedirectToAction(nameof(Index), new { episodeId });
				else if (movieId.HasValue)
					return RedirectToAction(nameof(Index), new { movieId });
				else
					return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				TempData["Error"] = $"Lỗi khi xóa: {ex.Message}";
				return RedirectToAction(nameof(Delete), new { id });
			}
		}

		private async Task PopulateDropdowns(Guid? selectedMovieId = null, Guid? selectedEpisodeId = null)
		{
			var movies = await _movieService.GetAllAsync();
			ViewBag.Movies = new SelectList(movies, "Id", "Title", selectedMovieId);

			if (selectedMovieId.HasValue)
			{
				var episodes = await _episodeService.GetByMovieIdAsync(selectedMovieId.Value);
				ViewBag.Episodes = new SelectList(
					episodes.Select(e => new {
						e.Id,
						Display = $"Tập {e.EpisodeNumber}: {e.Title}"
					}),
					"Id",
					"Display",
					selectedEpisodeId
				);
			}
			else
			{
				ViewBag.Episodes = new SelectList(Enumerable.Empty<object>(), "Id", "Display");
			}

			// Dropdown cho Quality
			ViewBag.Qualities = new SelectList(new[]
			{
				"360p", "480p", "720p", "1080p", "2160p"
			});

			// Dropdown cho FileType
			ViewBag.FileTypes = new SelectList(new[]
			{
				"Video", "Subtitle", "Trailer"
			});
		}
	}
}