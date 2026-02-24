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

		private readonly ILogger<MovieController> _logger;

		public MovieController(
			IMovieService movieService,
			IFavoriteService favoriteService,
			IReviewService reviewService,
			IEpisodeService episodeService,
			IMediaFileService mediaFileService,
			IGenreService genreService,
			ICountryService countryService,
			ILogger<MovieController> logger) 
		{
			_movieService = movieService;
			_favoriteService = favoriteService;
			_reviewService = reviewService;
			_episodeService = episodeService;
			_mediaFileService = mediaFileService;
			_genreService = genreService;
			_countryService = countryService;
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

			// Lấy tất cả phim và tìm theo slug
			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

			if (movie == null)
				return NotFound();

			// Lấy episodes nếu là phim bộ
			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);

			// Lấy media files
			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

			// Lấy phim liên quan
			var relatedMovies = allMovies
				.Where(m => m.IsActive &&
						   m.Id != movie.Id &&
						   (m.CountryId == movie.CountryId ||
							m.SelectedGenreIds.Intersect(movie.SelectedGenreIds).Any()))
				.OrderByDescending(m => m.ViewCount)
				.Take(6)
				.ToList();

			// Lấy genres và countries cho navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			// Kiểm tra favorite và reviews cho user hiện tại
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			ViewBag.IsFavorited = false;
			if (userId != null)
			{
				ViewBag.IsFavorited = await _favoriteService.IsFavoriteAsync(userId, movie.Id);
			}

			// Lấy rating và reviews
			try
			{
				var rating = await _reviewService.GetMovieRatingAsync(movie.Id);
				ViewBag.Rating = rating;
			}
			catch
			{
				ViewBag.Rating = null;
			}

			var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: true);
			ViewBag.Reviews = reviews.Take(5);

			ViewBag.UserReview = null;
			if (userId != null)
			{
				ViewBag.UserReview = await _reviewService.GetUserReviewForMovieAsync(userId, movie.Id);
			}

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
		public async Task<IActionResult> Watch(string slug, int? ep = null)
		{
			_logger.LogInformation(" WATCH ACTION START ");
			_logger.LogInformation($"Slug: {slug}, Episode: {ep}");

			if (string.IsNullOrEmpty(slug))
			{
				_logger.LogError("Slug is null or empty");
				TempData["Error"] = "Movie Not Found!";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				// Get all movies
				var allMovies = await _movieService.GetAllAsync();
				_logger.LogInformation($"Total movies: {allMovies.Count()}");

				// Find movie by slug
				var movie = allMovies.FirstOrDefault(m =>
					m.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) &&
					m.IsActive);

				if (movie == null)
				{
					_logger.LogError($"Movie not found with slug: {slug}");
					TempData["Error"] = $"Không tìm thấy phim: {slug}";
					return RedirectToAction("Index", "Home");
				}

				_logger.LogInformation($"Movie found: {movie.Title} (ID: {movie.Id})");

				// Initialize variables
				FinalCuongFilm.Common.DTOs.MediaFileDto? selectedMediaFile = null;
				FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;
				List<FinalCuongFilm.Common.DTOs.EpisodeDto> allEpisodes = new();

				// Handle Series vs Movie
				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
				{
					_logger.LogInformation("Processing SERIES");

					// Get all episodes
					var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
					allEpisodes = episodes
						.Where(e => e.IsActive)
						.OrderBy(e => e.EpisodeNumber)
						.ToList();

					_logger.LogInformation($"Found {allEpisodes.Count} episodes");

					if (!allEpisodes.Any())
					{
						_logger.LogWarning("No episodes found");
						TempData["Error"] = "Phim chưa có tập nào!";
						return RedirectToAction("Detail", new { slug });
					}

					// Determine current episode
					int targetEpisodeNumber = ep ?? 1;
					currentEpisode = allEpisodes
						.FirstOrDefault(e => e.EpisodeNumber == targetEpisodeNumber);

					if (currentEpisode == null)
					{
						_logger.LogWarning($"Episode {targetEpisodeNumber} not found, using first episode");
						currentEpisode = allEpisodes.First();
					}

					_logger.LogInformation($"Current episode: #{currentEpisode.EpisodeNumber} - {currentEpisode.Title}");

					// Get media file for this episode
					var episodeMediaFiles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
					selectedMediaFile = episodeMediaFiles
						.Where(m => m.FileType == "video")
						.OrderByDescending(m => m.Quality)
						.FirstOrDefault();

					_logger.LogInformation($"Episode has {episodeMediaFiles.Count()} media files");
				}
				else
				{
					_logger.LogInformation("Processing MOVIE");

					// Get media file for movie
					var movieMediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
					selectedMediaFile = movieMediaFiles
						.Where(m => m.FileType == "video")
						.OrderByDescending(m => m.Quality)
						.FirstOrDefault();

					_logger.LogInformation($"Movie has {movieMediaFiles.Count()} media files");
				}

				// Check if media file exists
				if (selectedMediaFile == null)
				{
					_logger.LogError("No video media file found!");
					TempData["Error"] = "Video chưa được upload! Vui lòng thử lại sau.";
					return RedirectToAction("Detail", new { slug });
				}

				_logger.LogInformation($"Selected media file: {selectedMediaFile.FileName}");
				_logger.LogInformation($"File URL: {selectedMediaFile.FileUrl}");
				_logger.LogInformation($"Quality: {selectedMediaFile.Quality}");

				// Set ViewBag data
				ViewBag.Movie = movie;
				ViewBag.MediaFile = selectedMediaFile;
				ViewBag.CurrentEpisode = currentEpisode;
				ViewBag.Episodes = allEpisodes;

				_logger.LogInformation("=== WATCH ACTION SUCCESS - Returning View ===");

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "EXCEPTION in Watch action");
				TempData["Error"] = $"Lỗi: {ex.Message}";
				return RedirectToAction("Index", "Home");
			}
		}

		// Tùy chọn: Giữ lại phương thức Watch cũ bằng ID nếu cần
		public async Task<IActionResult> WatchById(Guid id)
		{
			var movie = await _movieService.GetByIdAsync(id);
			if (movie == null || !movie.IsActive)
			{
				return NotFound();
			}

			// Redirect sang route dùng slug
			return RedirectToAction("Watch", new { slug = movie.Slug });
		}
	}
}