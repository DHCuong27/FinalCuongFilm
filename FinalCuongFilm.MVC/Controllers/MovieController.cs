using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class MovieController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IFavoriteService _favoriteService;
		private readonly IReviewService _reviewService;
		private readonly IEpisodeService _episodeService;
		private readonly IMediaFileService _mediaFileService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;
		private readonly IAzureBlobService _azureBlobService;
		private readonly ILogger<MovieController> _logger;

		public MovieController(
			IMovieService movieService,
			IFavoriteService favoriteService,
			IReviewService reviewService,
			IEpisodeService episodeService,
			IMediaFileService mediaFileService,
			IGenreService genreService,
			ICountryService countryService,
			IAzureBlobService azureBlobService,
			ILogger<MovieController> logger)
		{
			_movieService = movieService;
			_favoriteService = favoriteService;
			_reviewService = reviewService;
			_episodeService = episodeService;
			_mediaFileService = mediaFileService;
			_genreService = genreService;
			_countryService = countryService;
			_azureBlobService = azureBlobService;
			_logger = logger;
		}

		// GET: /Movies
		public async Task<IActionResult> Index()
		{
			var movies = await _movieService.GetAllAsync();
			return View(movies.Where(m => m.IsActive));
		}

		// GET: /Movies/Detail/{slug}
		public async Task<IActionResult> Detail(string slug)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

			if (movie == null)
				return NotFound();

			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

			var relatedMovies = allMovies
				.Where(m => m.IsActive &&
							m.Id != movie.Id &&
							(m.CountryId == movie.CountryId ||
							 m.SelectedGenreIds.Intersect(movie.SelectedGenreIds).Any()))
				.OrderByDescending(m => m.ViewCount)
				.Take(6)
				.ToList();

			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();
			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ViewBag.IsFavorited = false;
			if (userId != null)
				ViewBag.IsFavorited = await _favoriteService.IsFavoriteAsync(userId, movie.Id);

			try
			{
				ViewBag.Rating = await _reviewService.GetMovieRatingAsync(movie.Id);
			}
			catch
			{
				ViewBag.Rating = null;
			}

			var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: true);
			ViewBag.Reviews = reviews.Take(5);

			ViewBag.UserReview = null;
			if (userId != null)
				ViewBag.UserReview = await _reviewService.GetUserReviewForMovieAsync(userId, movie.Id);

			var viewModel = new MovieDetailsViewModel
			{
				Movie = movie,
				Episodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList(),
				MediaFiles = mediaFiles.ToList(),
				RelatedMovies = relatedMovies
			};

			return View(viewModel);
		}

		// GET: /Movie/Watch/{slug}?ep=1
		[AllowAnonymous]
		[Route("Movie/Watch/{slug}")]
		public async Task<IActionResult> Watch(string slug, int? ep = null)
		{
			_logger.LogInformation($"[Watch] slug={slug}, ep={ep}");

			if (string.IsNullOrEmpty(slug))
			{
				TempData["Error"] = "Movie not found.";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				var allMovies = await _movieService.GetAllAsync();
				var movie = allMovies.FirstOrDefault(m =>
					m.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && m.IsActive);

				if (movie == null)
				{
					TempData["Error"] = $"Movie not found: {slug}";
					return RedirectToAction("Index", "Home");
				}

				var genres = await _genreService.GetAllAsync();
				var countries = await _countryService.GetAllAsync();
				ViewBag.Genres = genres;
				ViewBag.Countries = countries;

				FinalCuongFilm.Common.DTOs.MediaFileDto? selectedMediaFile = null;
				FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;
				List<FinalCuongFilm.Common.DTOs.MediaFileDto> allQualityOptions = new();
				List<FinalCuongFilm.Common.DTOs.EpisodeDto> allEpisodes = new();

				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
				{
					var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
					allEpisodes = episodes
						.Where(e => e.IsActive)
						.OrderBy(e => e.EpisodeNumber)
						.ToList();

					if (!allEpisodes.Any())
					{
						TempData["Error"] = "This series has no episodes yet.";
						return RedirectToAction("Detail", new { slug });
					}

					int targetEp = ep ?? 1;
					currentEpisode = allEpisodes.FirstOrDefault(e => e.EpisodeNumber == targetEp)
									 ?? allEpisodes.First();

					var episodeMediaFiles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
					allQualityOptions = episodeMediaFiles
						.Where(m => m.FileType == "video")
						.OrderByDescending(m => m.Quality)
						.ToList();

					selectedMediaFile = allQualityOptions.FirstOrDefault();
				}
				else
				{
					var movieMediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
					allQualityOptions = movieMediaFiles
						.Where(m => m.FileType == "video")
						.OrderByDescending(m => m.Quality)
						.ToList();

					selectedMediaFile = allQualityOptions.FirstOrDefault();
				}

				if (selectedMediaFile == null)
				{
					TempData["Error"] = "Video not available yet. Please try again later.";
					return RedirectToAction("Detail", new { slug });
				}

				// FIX #2a: Tạo SAS URL có thời hạn 4 giờ thay vì lộ raw Azure Blob URL
				var streamingUrl = await _azureBlobService.GetStreamingUrlAsync(
					selectedMediaFile.FileUrl, expiryHours: 4);

				// FIX #2b: Tạo SAS URL cho tất cả quality options
				var qualitySources = new List<object>();
				foreach (var qf in allQualityOptions)
				{
					var qUrl = await _azureBlobService.GetStreamingUrlAsync(qf.FileUrl, expiryHours: 4);
					qualitySources.Add(new
					{
						id = qf.Id,
						quality = qf.Quality ?? "Auto",
						url = qUrl
					});
				}

				// FIX #2c: Lấy subtitles nếu có
				var subtitleFiles = new List<object>();
				IEnumerable<FinalCuongFilm.Common.DTOs.MediaFileDto> allMediaForSubtitles;
				if (currentEpisode != null)
					allMediaForSubtitles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
				else
					allMediaForSubtitles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

				foreach (var sub in allMediaForSubtitles.Where(m => m.FileType == "subtitle"))
				{
					var subUrl = await _azureBlobService.GetStreamingUrlAsync(sub.FileUrl, expiryHours: 4);
					subtitleFiles.Add(new
					{
						language = sub.Language ?? "Unknown",
						url = subUrl,
						label = sub.Language ?? "Subtitle"
					});
				}

				// FIX #2d: Tăng ViewCount
				await _movieService.IncrementViewCountAsync(movie.Id);

				// Truyền vào View — streamingUrl (SAS) thay vì raw FileUrl
				ViewBag.Movie = movie;
				ViewBag.StreamingUrl = streamingUrl;       // SAS URL — có thời hạn
				ViewBag.MediaFile = selectedMediaFile;      // Metadata (không chứa raw URL nữa)
				ViewBag.QualitySources = qualitySources;    // Tất cả quality options (SAS URLs)
				ViewBag.SubtitleFiles = subtitleFiles;      // Subtitle tracks
				ViewBag.CurrentEpisode = currentEpisode;
				ViewBag.Episodes = allEpisodes;
				ViewBag.MovieId = movie.Id;
				ViewBag.EpisodeId = currentEpisode?.Id;

				_logger.LogInformation($"[Watch] Success: {movie.Title}, quality options: {qualitySources.Count}");
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Watch] Exception");
				TempData["Error"] = $"Error: {ex.Message}";
				return RedirectToAction("Index", "Home");
			}
		}

		public async Task<IActionResult> WatchById(Guid id)
		{
			var movie = await _movieService.GetByIdAsync(id);
			if (movie == null || !movie.IsActive)
				return NotFound();

			return RedirectToAction("Watch", new { slug = movie.Slug });
		}
	}
}