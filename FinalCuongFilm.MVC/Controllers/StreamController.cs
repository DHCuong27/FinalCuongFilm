//using Microsoft.AspNetCore.Mvc;
//using FinalCuongFilm.Service.Interfaces;
//using System.Security.Claims;

//namespace FinalCuongFilm.MVC.Controllers
//{
//	public class StreamController : Controller
//	{
//		private readonly IMediaFileService _mediaFileService;
//		private readonly ILogger<StreamController> _logger;

//		public StreamController(
//			IMediaFileService mediaFileService,
//			ILogger<StreamController> logger)
//		{
//			_mediaFileService = mediaFileService;
//			_logger = logger;
//		}

//		// GET: /Stream/Video/{mediaFileId}
//		[HttpGet]
//		public async Task<IActionResult> Video(Guid mediaFileId)
//		{
//			try
//			{
//				var mediaFile = await _mediaFileService.GetByIdAsync(mediaFileId);

//				if (mediaFile == null)
//				{
//					return Json(new { success = false, message = "Media file not found" });
//				}

//				return Json(new
//				{
//					success = true,
//					url = mediaFile.FileUrl,
//					quality = mediaFile.Quality,
//					language = mediaFile.Language
//				});
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "Error getting video URL");
//				return Json(new { success = false, message = ex.Message });
//			}
//		}

//		// POST: /Stream/UpdateProgress
//		[HttpPost]
//		public IActionResult UpdateProgress([FromBody] ProgressUpdateModel model)
//		{
//			// TODO: Implement save to database if needed
//			return Json(new { success = true });
//		}

//		// GET: /Stream/Progress/{movieId}
//		[HttpGet]
//		public IActionResult Progress(Guid movieId, Guid? episodeId = null)
//		{
//			// TODO: Implement load from database if needed
//			return Json(new { success = true, currentTime = 0 });
//		}
//	}

//	public class ProgressUpdateModel
//	{
//		public Guid MovieId { get; set; }
//		public Guid? EpisodeId { get; set; }
//		public double CurrentTime { get; set; }
//		public double Duration { get; set; }
//	}
//}