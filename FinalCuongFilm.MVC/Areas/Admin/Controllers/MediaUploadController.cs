using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

		// GET: Admin/MediaUpload/Index
		public async Task<IActionResult> Index(Guid? movieId, Guid? episodeId, string fileType = null)
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

			// Filter by file type if specified
			if (!string.IsNullOrEmpty(fileType))
			{
				mediaFiles = mediaFiles.Where(m => m.FileType == fileType);
			}

			ViewBag.FileType = fileType;
			return View(mediaFiles);
		}

		// GET: Admin/MediaUpload/Details/{id}
		public async Task<IActionResult> Details(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null)
			{
				return NotFound();
			}

			return View(mediaFile);
		}

		// GET: Admin/MediaUpload/Edit/{id}
		public async Task<IActionResult> Edit(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null)
			{
				return NotFound();
			}

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
			if (id != dto.Id)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					await _mediaFileService.UpdateAsync(dto);
					TempData["Success"] = "Cập nhật file thành công!";

					return RedirectToAction(nameof(Index), new { movieId = dto.MovieId });
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error updating media file {Id}", id);
					ModelState.AddModelError("", $"Lỗi: {ex.Message}");
				}
			}

			await PopulateDropdowns(dto.MovieId, dto.EpisodeId);
			return View(dto);
		}

		// GET: Admin/MediaUpload/Delete/{id}
		public async Task<IActionResult> Delete(Guid id)
		{
			var mediaFile = await _mediaFileService.GetByIdAsync(id);
			if (mediaFile == null)
			{
				return NotFound();
			}

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
				if (mediaFile == null)
				{
					return NotFound();
				}

				// Delete from Azure Blob Storage
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
						// Continue to delete from DB even if Azure deletion fails
					}
				}

				// Delete from database
				await _mediaFileService.DeleteAsync(id);

				TempData["Success"] = "Xóa file thành công!";

				return RedirectToAction(nameof(Index), new { movieId = mediaFile.MovieId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting media file {Id}", id);
				TempData["Error"] = $"Lỗi khi xóa file: {ex.Message}";
				return RedirectToAction(nameof(Delete), new { id });
			}
		}

		private async Task PopulateDropdowns(Guid? movieId, Guid? episodeId)
		{
			var movies = await _movieService.GetAllAsync();
			ViewBag.MovieId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(movies, "Id", "Title", movieId);

			if (movieId.HasValue)
			{
				var episodes = await _episodeService.GetByMovieIdAsync(movieId.Value);
				ViewBag.EpisodeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
				episodes.Select(e => new {
					e.Id,
					Display = $"Tập {e.EpisodeNumber}: {e.Title}"
				}),
				"Id",
				"Display",
				episodeId
				);
			}
			else
			{
				ViewBag.EpisodeId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(System.Linq.Enumerable.Empty<object>(), "Id", "Display");
			}
		}


		private string NormalizeAzureUrl(string url)
		{
			if (string.IsNullOrEmpty(url)) return url;
			if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				url = "https://" + url;
			}
			return url.Trim();
		}

		// GET: Admin/MediaUpload/UploadVideo
		public async Task<IActionResult> UploadVideo(Guid? movieId = null)
		{
			var movies = await _movieService.GetAllAsync();
			ViewBag.Movies = movies;
			ViewBag.SelectedMovieId = movieId;

			return View();
		}


		// POST: Admin/MediaUpload/UploadVideo
		[HttpPost]
		[RequestSizeLimit(5_000_000_000)] 
		[RequestFormLimits(MultipartBodyLengthLimit = 5_000_000_000)]
		public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto dto)
		{
			if (!ModelState.IsValid) return Json(new { success = false, message = "Invalid data" });

			try
			{
				var movie = await _movieService.GetByIdAsync(dto.MovieId);
				if (movie == null) return Json(new { success = false, message = "Movie not found" });

				// Tự động tìm EpisodeId nếu là phim Series
				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series && dto.EpisodeNumber.HasValue)
				{
					if (dto.EpisodeId == null)
					{
						var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
						var targetEp = episodes.FirstOrDefault(e => e.EpisodeNumber == dto.EpisodeNumber.Value);
						if (targetEp != null) dto.EpisodeId = targetEp.Id;
						else return Json(new { success = false, message = $"Episode {dto.EpisodeNumber} not created yet." });
					}
				}

				//  Nhập link thủ công (Manual URL)
				if (!string.IsNullOrEmpty(dto.ManualUrl))
				{
					string hlsUrl = NormalizeAzureUrl(dto.ManualUrl);
					_logger.LogInformation("Using manual URL: {Url}", hlsUrl);

					var manualMediaDto = new MediaFileCreateDto
					{
						FileName = "Manual URL",
						FileUrl = hlsUrl,
						FileType = "hls", // Mặc định coi link ngoài là luồng HLS
						Quality = dto.Quality ?? "Auto",
						Language = dto.Language ?? "vi",
						FileSizeBytes = 0,
						MovieId = dto.MovieId,
						EpisodeId = dto.EpisodeId
					};
					await _mediaFileService.CreateAsync(manualMediaDto);

					return Json(new { success = true, message = "Lưu URL thủ công thành công!" });
				}

				// Upload file Video từ máy tính
				else if (dto.VideoFile != null && dto.VideoFile.Length > 0)
				{
					// 1. Chỉ upload MP4 gốc lên Azure 
					string originalUrl = await _azureBlobService.UploadVideoAsync(dto.VideoFile, movie.Slug, dto.EpisodeNumber);
					long fileSize = dto.VideoFile.Length;

					// 2. Lưu Database NGAY LẬP TỨC với thông tin của MP4 gốc trước
					var mediaFileDto = new MediaFileCreateDto
					{
						FileName = dto.VideoFile.FileName,
						FileUrl = originalUrl,
						FileType = "video", // video gốc chờ xử lý
						Quality = "Processing...", // Đánh dấu đang xử lý
						Language = dto.Language ?? "vi",
						FileSizeBytes = fileSize,
						MovieId = dto.MovieId,
						EpisodeId = dto.EpisodeId
					};
					var createdMedia = await _mediaFileService.CreateAsync(mediaFileDto);

					// 3. ĐẨY JOB VÀO HANGFIRE
					BackgroundJob.Enqueue<IVideoConversionService>(service =>
						service.ProcessVideoBackgroundJobAsync(createdMedia.Id, originalUrl, movie.Slug, dto.EpisodeNumber ?? 1));

					// 4. Trả về kết quả ngay cho giao diện
					return Json(new { success = true, message = "Video has been uploaded! The system is compressing HLS in the background; you can close this notification." });
				}

				
				else
				{
					return Json(new { success = false, message = "Please select the video file." });
				}
			} 
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing video");
				return Json(new { success = false, message = "Error: " + ex.Message });
			}
		}

		// POST: Admin/MediaUpload/UploadPoster
		[HttpPost]
		public async Task<IActionResult> UploadPoster(IFormFile posterFile, Guid movieId)
		{
			try
			{
				var movie = await _movieService.GetByIdAsync(movieId);
				if (movie == null)
				{
					return Json(new { success = false, message = "Film not found." });
				}

				var posterUrl = await _azureBlobService.UploadPosterAsync(posterFile, movie.Slug);

				// Update movie poster URL
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
					message = "Upload poster thành công!",
					posterUrl = posterUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading poster");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// GET: Admin/MediaUpload/TestConnection
		public async Task<IActionResult> TestConnection()
		{
			try
			{
				// Create a test text file
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
					message = "Kết nối Azure Blob Storage thành công!",
					testUrl = testUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Azure connection test failed");
				return Json(new
				{
					success = false,
					message = "Error Connect: " + ex.Message,
					details = ex.InnerException?.Message
				});
			}
		}
	}

	public class VideoUploadDto
	{
		public Guid MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
		public int? EpisodeNumber { get; set; }
		public IFormFile? VideoFile { get; set; }
		public string? ManualUrl { get; set; }
		public string Quality { get; set; } = "1080p";
		public string? Language { get; set; }
	}
}