using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class MediaUploadController : Controller
	{
		private readonly IAzureBlobService _azureBlobService;
		private readonly IMediaFileService _mediaFileService;
		private readonly IMovieService _movieService;
		private readonly ILogger<MediaUploadController> _logger;

		public MediaUploadController(
			IAzureBlobService azureBlobService,
			IMediaFileService mediaFileService,
			IMovieService movieService,
			ILogger<MediaUploadController> logger)
		{
			_azureBlobService = azureBlobService;
			_mediaFileService = mediaFileService;
			_movieService = movieService;
			_logger = logger;
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
		[RequestSizeLimit(5_000_000_000)] // 5GB
		[RequestFormLimits(MultipartBodyLengthLimit = 5_000_000_000)]
		public async Task<IActionResult> UploadVideo([FromForm] VideoUploadDto dto)
		{
			if (!ModelState.IsValid)
			{
				return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
			}

			try
			{
				_logger.LogInformation("Starting video upload for movie {MovieId}", dto.MovieId);

				var movie = await _movieService.GetByIdAsync(dto.MovieId);
				if (movie == null)
				{
					return Json(new { success = false, message = "Không tìm thấy phim" });
				}

				// Upload video to Azure
				var videoUrl = await _azureBlobService.UploadVideoAsync(
					dto.VideoFile,
					movie.Slug,
					dto.EpisodeNumber
				);

				// Save to database
				var mediaFileDto = new MediaFileCreateDto
				{
					FileName = dto.VideoFile.FileName,
					FileUrl = videoUrl,
					FileType = "video",
					Quality = dto.Quality,
					Language = dto.Language ?? "vi",
					FileSizeBytes = dto.VideoFile.Length,
					MovieId = dto.MovieId,
					EpisodeId = dto.EpisodeId
				};

				await _mediaFileService.CreateAsync(mediaFileDto);

				_logger.LogInformation("Video uploaded successfully: {VideoUrl}", videoUrl);

				return Json(new
				{
					success = true,
					message = "Upload video thành công!",
					videoUrl = videoUrl
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading video");
				return Json(new
				{
					success = false,
					message = "Lỗi upload: " + ex.Message
				});
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
					return Json(new { success = false, message = "Không tìm thấy phim" });
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

				await _movieService.UpdateAsync(updateDto);

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
					message = "Lỗi kết nối: " + ex.Message,
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
		public IFormFile VideoFile { get; set; } = null!;
		public string Quality { get; set; } = "1080p";
		public string? Language { get; set; }
	}
}