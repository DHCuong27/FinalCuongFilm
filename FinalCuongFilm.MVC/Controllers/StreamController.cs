using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FinalCuongFilm.Service.Interfaces;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class StreamController : Controller
	{
		private readonly IMediaFileService _mediaFileService;
		private readonly IAzureBlobService _azureBlobService;
		private readonly ILogger<StreamController> _logger;

		public StreamController(
			IMediaFileService mediaFileService,
			IAzureBlobService azureBlobService,
			ILogger<StreamController> logger)
		{
			_mediaFileService = mediaFileService;
			_azureBlobService = azureBlobService;
			_logger = logger;
		}

		/// <summary>
		/// GET: /Stream/Video/{mediaFileId}
		/// Generate streaming URL with SAS token
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Video(Guid mediaFileId)
		{
			try
			{
				var mediaFile = await _mediaFileService.GetByIdAsync(mediaFileId);

				if (mediaFile == null)
				{
					return NotFound(new { message = "Video không tồn tại" });
				}

				// Generate streaming URL with SAS token
				var streamingUrl = await _azureBlobService.GetStreamingUrlAsync(mediaFile.FileUrl, expiryHours: 24);

				return Json(new
				{
					success = true,
					url = streamingUrl,
					fileName = mediaFile.FileName,
					quality = mediaFile.Quality,
					//duration = mediaFile.DurationSeconds,
					expiresIn = 24 * 3600 // seconds
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting video stream for MediaFileId: {MediaFileId}", mediaFileId);
				return StatusCode(500, new { success = false, message = "Lỗi khi tải video" });
			}
		}

		/// <summary>
		/// GET: /Stream/Subtitle/{mediaFileId}
		/// Get subtitle URL
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Subtitle(Guid mediaFileId, string language = "vi")
		{
			try
			{
				var subtitles = await _mediaFileService.GetSubtitlesAsync(mediaFileId, language);

				if (subtitles == null)
				{
					return NotFound(new { message = "Phụ đề không tồn tại" });
				}

				var subtitleUrl = await _azureBlobService.GetStreamingUrlAsync(subtitles.FileUrl, expiryHours: 24);

				return Json(new
				{
					success = true,
					url = subtitleUrl,
					language = language
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting subtitle for MediaFileId: {MediaFileId}", mediaFileId);
				return Json(new { success = false, message = "Không tìm thấy phụ đề" });
			}
		}

		/// <summary>
		/// POST: /Stream/UpdateProgress
		/// Save watching progress (placeholder - cần WatchHistory service)
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<IActionResult> UpdateProgress([FromBody] WatchProgressDto dto)
		{
			try
			{
				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
				if (string.IsNullOrEmpty(userId))
				{
					return Unauthorized();
				}

				// TODO: Implement WatchHistory service
				// await _watchHistoryService.SaveProgressAsync(userId, dto);

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating watch progress");
				return Json(new { success = false });
			}
		}
	}

	public class WatchProgressDto
	{
		public Guid MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
		public double CurrentTime { get; set; }
		public double Duration { get; set; }
	}
}