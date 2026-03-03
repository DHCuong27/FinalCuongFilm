using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class StreamController : Controller
	{
		private readonly IMediaFileService _mediaFileService;
		private readonly IAzureBlobService _azureBlobService;
		private readonly IMovieService _movieService;
		private readonly IEpisodeService _episodeService;
		private readonly ILogger<StreamController> _logger;

		public StreamController(
			IMediaFileService mediaFileService,
			IAzureBlobService azureBlobService,
			IMovieService movieService,
			IEpisodeService episodeService,
			ILogger<StreamController> logger)
		{
			_mediaFileService = mediaFileService;
			_azureBlobService = azureBlobService;
			_movieService = movieService;
			_episodeService = episodeService;
			_logger = logger;
		}

		// GET: /Stream/Video/{mediaFileId}
		// Trả về SAS URL có thời hạn — không lộ raw blob URL ra client
		[HttpGet]
		public async Task<IActionResult> Video(Guid mediaFileId)
		{
			try
			{
				var mediaFile = await _mediaFileService.GetByIdAsync(mediaFileId);

				if (mediaFile == null)
				{
					_logger.LogWarning($"Media file not found: {mediaFileId}");
					return Json(new { success = false, message = "Media file not found" });
				}

				// Tạo SAS token có thời hạn 4 giờ — không lộ raw URL
				var streamingUrl = await _azureBlobService.GetStreamingUrlAsync(mediaFile.FileUrl, expiryHours: 4);

				_logger.LogInformation($"Generated streaming URL for media: {mediaFileId}");

				return Json(new
				{
					success = true,
					url = streamingUrl,
					quality = mediaFile.Quality,
					language = mediaFile.Language,
					fileName = mediaFile.FileName
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting streaming URL for: {mediaFileId}");
				return Json(new { success = false, message = "Unable to load video. Please try again." });
			}
		}

		// GET: /Stream/Sources/{movieId}?ep=1
		// Trả về tất cả quality options cho movie/episode (dùng cho quality switcher)
		[HttpGet]
		public async Task<IActionResult> Sources(Guid movieId, int? ep = null)
		{
			try
			{
				IEnumerable<FinalCuongFilm.Common.DTOs.MediaFileDto> mediaFiles;

				if (ep.HasValue)
				{
					// Lấy media files theo episode
					var episodes = await _episodeService.GetByMovieIdAsync(movieId);
					var episode = episodes.FirstOrDefault(e => e.EpisodeNumber == ep.Value);
					if (episode == null)
						return Json(new { success = false, message = "Episode not found" });

					mediaFiles = await _mediaFileService.GetByEpisodeIdAsync(episode.Id);
				}
				else
				{
					mediaFiles = await _mediaFileService.GetByMovieIdAsync(movieId);
				}

				var videoFiles = mediaFiles
					.Where(m => m.FileType == "video")
					.OrderByDescending(m => m.Quality)
					.Select(m => new
					{
						id = m.Id,
						quality = m.Quality ?? "Auto",
						language = m.Language ?? "Default"
					})
					.ToList();

				return Json(new { success = true, sources = videoFiles });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting video sources");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// POST: /Stream/Progress
		// Lưu vị trí xem của user vào Session (không cần DB cho MVP)
		[HttpPost]
		public IActionResult Progress([FromBody] ProgressUpdateModel model)
		{
			try
			{
				if (model.Duration <= 0) return Json(new { success = false });

				// Lưu vào Session với key: progress_{movieId}_{episodeId}
				var key = $"progress_{model.MovieId}_{model.EpisodeId}";
				HttpContext.Session.SetString(key, model.CurrentTime.ToString("F2"));

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saving progress");
				return Json(new { success = false });
			}
		}

		// GET: /Stream/Progress/{movieId}?episodeId=...
		// Lấy lại vị trí xem từ Session
		[HttpGet]
		public IActionResult GetProgress(Guid movieId, Guid? episodeId = null)
		{
			try
			{
				var key = $"progress_{movieId}_{episodeId}";
				var saved = HttpContext.Session.GetString(key);
				var currentTime = saved != null ? double.Parse(saved) : 0;

				return Json(new { success = true, currentTime });
			}
			catch
			{
				return Json(new { success = true, currentTime = 0 });
			}
		}
	}

	public class ProgressUpdateModel
	{
		public Guid MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
		public double CurrentTime { get; set; }
		public double Duration { get; set; }
	}
}