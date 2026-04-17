using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class MediaUploadController : Controller
	{
		private readonly IAzureBlobService _azureBlobService;
		private readonly IMediaFileService _mediaFileService;
		private readonly IMovieService _movieService;
		private readonly IEpisodeService _episodeService;
		private readonly IVideoConversionService _videoConversionService;
		private readonly ILogger<MediaUploadController> _logger;

		public MediaUploadController(
			IAzureBlobService azureBlobService,
			IMediaFileService mediaFileService,
			IMovieService movieService,
			IEpisodeService episodeService,
			IVideoConversionService videoConversionService,
			ILogger<MediaUploadController> logger)
		{
			_azureBlobService = azureBlobService;
			_mediaFileService = mediaFileService;
			_movieService = movieService;
			_episodeService = episodeService;
			_videoConversionService = videoConversionService;
			_logger = logger;
		}

		#region 1. GLOBAL MEDIA MANAGEMENT (Quản lý toàn bộ danh sách file)

		// GET: Admin/MediaUpload/Index
		public async Task<IActionResult> Index(Guid? movieId, Guid? episodeId, string fileType = null, int page = 1)
		{
			int pageSize = 10;
			IEnumerable<MediaFileDto> mediaFiles;

			if (episodeId.HasValue)
			{
				mediaFiles = await _mediaFileService.GetByEpisodeIdAsync(episodeId.Value);
				var episode = await _episodeService.GetByIdAsync(episodeId.Value);
				ViewBag.EpisodeTitle = $"Episode {episode?.EpisodeNumber}: {episode?.Title}";
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

			if (!string.IsNullOrEmpty(fileType))
			{
				mediaFiles = mediaFiles.Where(m => m.FileType == fileType);
			}

			// Xử lý phân trang
			int totalItems = mediaFiles.Count();
			int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

			page = page < 1 ? 1 : page;
			page = page > totalPages && totalPages > 0 ? totalPages : page;

			var pagedMediaFiles = mediaFiles
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.TotalItems = totalItems;
			ViewBag.FileType = fileType;

			return View(pagedMediaFiles);
		}

		// GET: Admin/MediaUpload/Details/{id}
		public async Task<IActionResult> Details(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null) return NotFound();

			return View(mediaFile);
		}

		// GET: Admin/MediaUpload/Edit/{id}
		public async Task<IActionResult> Edit(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null) return NotFound();

			var dto = new MediaFileUpdateDto
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
			return View(dto);
		}

		// POST: Admin/MediaUpload/Edit/{id}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(Guid id, MediaFileUpdateDto dto)
		{
			if (id != dto.Id) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					await _mediaFileService.UpdateAsync(dto);
					TempData["Success"] = "Media file updated successfully!";
					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating media file {Id}", id);
					ModelState.AddModelError("", $"Error: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.MovieId, dto.EpisodeId);
			return View(dto);
		}

		// GET: Admin/MediaUpload/Delete/{id}
		public async Task<IActionResult> Delete(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null) return NotFound();

			return View(mediaFile);
		}

		// POST: Admin/MediaUpload/Delete/{id}
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(Guid id)
		{
			try
			{
				var mediaFile = await _mediaFileService.GetByIdAsync(id);
				if (mediaFile == null) return NotFound();

				// Xóa file vật lý trên Azure
				if (!string.IsNullOrEmpty(mediaFile.FileUrl))
				{
					try
					{
						await _azureBlobService.DeleteFileAsync(mediaFile.FileUrl);
						_logger.LogInformation("Deleted file from Azure: {FileUrl}", mediaFile.FileUrl);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to delete file from Azure: {FileUrl}", mediaFile.FileUrl);
					}
				}

				// Xóa record trong DB
				await _mediaFileService.DeleteAsync(id);
				TempData["Success"] = "Media file deleted successfully!";

				return RedirectToAction(nameof(Index), new { movieId = mediaFile.MovieId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting media file {Id}", id);
				TempData["Error"] = $"Error deleting file: {ex.Message}";
				return RedirectToAction(nameof(Delete), new { id });
			}
		}

		#endregion

		#region 2. MOVIE-SPECIFIC UPLOAD (Luồng giao diện Upload chuẩn Netflix)

		[HttpGet]
		public async Task<IActionResult> UploadVideo(Guid? id)
		{
			// Bảo vệ UX: Tránh văng lỗi 404 nếu Admin truy cập URL không có ID
			if (id == null)
			{
				TempData["Error"] = "Please select a movie from the list to manage videos.";
				return RedirectToAction("Index", "Movies", new { area = "Admin" });
			}

			var movie = await _movieService.GetByIdAsync(id.Value);
			if (movie == null) return NotFound();

			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(id.Value);
			ViewBag.MediaFiles = mediaFiles;

			return View(movie);
		}

		[HttpPost]
		[RequestSizeLimit(5_000_000_000)]
		[RequestFormLimits(MultipartBodyLengthLimit = 5_000_000_000)]
		public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto dto)
		{
			if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data." });

			try
			{
				var movie = await _movieService.GetByIdAsync(dto.MovieId);
				if (movie == null) return Json(new { success = false, message = "Movie not found." });

				// Tự động map EpisodeId nếu là phim bộ
				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series && dto.EpisodeNumber.HasValue)
				{
					if (dto.EpisodeId == null)
					{
						var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
						var targetEp = episodes.FirstOrDefault(e => e.EpisodeNumber == dto.EpisodeNumber.Value);

						if (targetEp != null) dto.EpisodeId = targetEp.Id;
						else return Json(new { success = false, message = $"Episode {dto.EpisodeNumber} has not been created yet." });
					}
				}

				// Xử lý upload file MP4 từ Local
				if (dto.VideoFile != null && dto.VideoFile.Length > 0)
				{
					string originalUrl = await _azureBlobService.UploadVideoAsync(dto.VideoFile, movie.Slug, dto.EpisodeNumber);
					long fileSize = dto.VideoFile.Length;

					var mediaFileDto = new MediaFileCreateDto
					{
						FileName = dto.VideoFile.FileName,
						FileUrl = originalUrl,
						FileType = "video",
						Quality = "Auto HLS",
						Language = dto.Language ?? "vi",
						FileSizeBytes = fileSize,
						MovieId = dto.MovieId,
						EpisodeId = dto.EpisodeId
					};

					var createdMedia = await _mediaFileService.CreateAsync(mediaFileDto);

					// Bắn Job nén HLS ngầm vào Hangfire
					BackgroundJob.Enqueue<IVideoConversionService>(service =>
						service.ProcessVideoBackgroundJobAsync(createdMedia.Id, originalUrl, movie.Slug, dto.EpisodeNumber ?? 1));

					return Json(new { success = true, message = "Upload complete! The system is automatically compressing HLS in the background." });
				}
				else
				{
					return Json(new { success = false, message = "Please select a valid video file." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing video");
				return Json(new { success = false, message = "Processing error: " + ex.Message });
			}
		}

		// Hàm xóa Video dùng riêng cho giao diện UploadVideo
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteVideo(Guid mediaId, Guid movieId)
		{
			try
			{
				var mediaFile = await _mediaFileService.GetByIdAsync(mediaId);
				if (mediaFile != null)
				{
					if (!string.IsNullOrEmpty(mediaFile.FileUrl))
					{
						await _azureBlobService.DeleteFileAsync(mediaFile.FileUrl);
					}
					await _mediaFileService.DeleteAsync(mediaId);
					TempData["Success"] = "Media file deleted successfully!";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting media {Id}", mediaId);
				TempData["Error"] = "Error deleting file: " + ex.Message;
			}

			return RedirectToAction(nameof(UploadVideo), new { id = movieId });
		}

		#endregion

		#region 3. HELPERS & OTHER ACTIONS

		[HttpPost]
		public async Task<IActionResult> UploadPoster(IFormFile posterFile, Guid movieId)
		{
			try
			{
				var movie = await _movieService.GetByIdAsync(movieId);
				if (movie == null)
				{
					return Json(new { success = false, message = "Movie not found." });
				}

				var posterUrl = await _azureBlobService.UploadPosterAsync(posterFile, movie.Slug);

				var updateDto = new MovieUpdateDto
				{
					Id = movie.Id,
					Title = movie.Title,
					Description = movie.Description,
					PosterUrl = posterUrl,
					ReleaseYear = movie.ReleaseYear,
					DurationMinutes = movie.DurationMinutes,
					Type = movie.Type,
					Status = movie.Status,
					IsActive = movie.IsActive,
					LanguageId = movie.LanguageId,
					CountryId = movie.CountryId,
					ActorIds = movie.SelectedActorIds,
					GenreIds = movie.SelectedGenreIds
				};

				await _movieService.UpdateAsync(updateDto.Id, updateDto);

				return Json(new
				{
					success = true,
					message = "Poster uploaded successfully!",
					posterUrl = posterUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading poster");
				return Json(new { success = false, message = ex.Message });
			}
		}

		public async Task<IActionResult> TestConnection()
		{
			try
			{
				var testContent = "Azure Blob Storage connection test - " + DateTime.Now;
				var bytes = System.Text.Encoding.UTF8.GetBytes(testContent);

				using var stream = new MemoryStream(bytes);
				var formFile = new FormFile(stream, 0, bytes.Length, "test", "test.txt")
				{
					Headers = new HeaderDictionary(),
					ContentType = "text/plain"
				};

				var testUrl = await _azureBlobService.UploadAsync(formFile, "videos", "test/connection-test.txt");

				return Json(new
				{
					success = true,
					message = "Azure Blob Storage connection successful!",
					testUrl = testUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Azure connection test failed");
				return Json(new
				{
					success = false,
					message = "Connection Error: " + ex.Message,
					details = ex.InnerException?.Message
				});
			}
		}

		private async Task PopulateDropdowns(Guid? movieId, Guid? episodeId)
		{
			var movies = await _movieService.GetAllAsync();
			ViewBag.MovieId = new SelectList(movies, "Id", "Title", movieId);

			if (movieId.HasValue)
			{
				var episodes = await _episodeService.GetByMovieIdAsync(movieId.Value);
				ViewBag.EpisodeId = new SelectList(
					episodes.Select(e => new {
						e.Id,
						Display = $"Episode {e.EpisodeNumber}: {e.Title}"
					}),
					"Id",
					"Display",
					episodeId
				);
			}
			else
			{
				ViewBag.EpisodeId = new SelectList(System.Linq.Enumerable.Empty<object>(), "Id", "Display");
			}
		}

		#endregion
	}
}