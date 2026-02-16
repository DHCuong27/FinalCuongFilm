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

		public MovieController(
			IMovieService movieService,
			IFavoriteService favoriteService,
			IReviewService reviewService,
			IEpisodeService episodeService,
			IMediaFileService mediaFileService,
			IGenreService genreService,
			ICountryService countryService)
		{
			_movieService = movieService;
			_favoriteService = favoriteService;
			_reviewService = reviewService;
			_episodeService = episodeService;
			_mediaFileService = mediaFileService;
			_genreService = genreService;
			_countryService = countryService;
		}

		// GET: /Movies
		public async Task<IActionResult> Index()
		{
			var movies = await _movieService.GetAllAsync();
			return View(movies.Where(m => m.IsActive));
		}

		// GET: /Movies/Details/{id} - Giữ lại cho API hoặc back-office
		public async Task<IActionResult> Details(Guid id)
		{
			var movie = await _movieService.GetByIdAsync(id);
			if (movie == null || !movie.IsActive)
			{
				return NotFound();
			}

			// Get user ID
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

			// Check if user favorited this movie
			ViewBag.IsFavorited = false;
			if (userId != null)
			{
				ViewBag.IsFavorited = await _favoriteService.IsFavoriteAsync(userId, id);
			}

			// Get movie rating and reviews
			try
			{
				var rating = await _reviewService.GetMovieRatingAsync(id);
				ViewBag.Rating = rating;
			}
			catch
			{
				ViewBag.Rating = null;
			}

			// Get approved reviews
			var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: false);
			ViewBag.Reviews = reviews.Take(5);

			// Check if user already reviewed
			ViewBag.UserReview = null;
			if (userId != null)
			{
				ViewBag.UserReview = await _reviewService.GetUserReviewForMovieAsync(userId, id);
			}

			// Get episodes (if series)
			if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
			{
				var episodes = await _episodeService.GetByMovieIdAsync(id);
				ViewBag.Episodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber);
			}

			return View(movie);
		}

		// GET: /Movies/Detail/{slug} - Đã hợp nhất từ MovieController
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
			// ✅ LOG để debug
			Console.WriteLine("========================================");
			Console.WriteLine($"[WATCH] Action called with slug: {slug}, ep: {ep}");
			Console.WriteLine("========================================");

			if (string.IsNullOrEmpty(slug))
			{
				Console.WriteLine("[ERROR] Slug is null or empty");
				TempData["Error"] = "Không tìm thấy phim!";
				return RedirectToAction("Index", "Home");
			}

			try
			{
				// Get movie by slug
				var allMovies = await _movieService.GetAllAsync();
				Console.WriteLine($"[DEBUG] Total movies in database: {allMovies.Count()}");

				var movie = allMovies.FirstOrDefault(m =>
					m.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase) && m.IsActive);

				if (movie == null)
				{
					Console.WriteLine($"[ERROR] Movie not found with slug: '{slug}'");
					Console.WriteLine("[DEBUG] Available slugs (first 10):");

					foreach (var m in allMovies.Where(m => m.IsActive).Take(10))
					{
						Console.WriteLine($"  - '{m.Slug}' (Title: {m.Title})");
					}

					TempData["Error"] = $"Không tìm thấy phim với slug: {slug}";
					return RedirectToAction("Index", "Home");
				}

				Console.WriteLine($"[SUCCESS] Movie found: {movie.Title} (ID: {movie.Id}, Type: {movie.Type})");

				// ✅ SET ViewBag NGAY LẬP TỨC
				ViewBag.Movie = movie;

				FinalCuongFilm.Common.DTOs.MediaFileDto? mediaFile = null;
				FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;
				List<FinalCuongFilm.Common.DTOs.EpisodeDto> episodesList = new();

				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
				{
					Console.WriteLine("[INFO] Processing Series type movie");

					// Get episodes
					var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
					episodesList = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList();

					Console.WriteLine($"[DEBUG] Found {episodesList.Count} active episodes");

					if (!episodesList.Any())
					{
						Console.WriteLine("[WARNING] No episodes found for this series");
						TempData["Error"] = "Phim chưa có tập nào!";
						return RedirectToAction("Detail", new { slug });
					}

					// Get current episode
					var episodeNumber = ep ?? 1;
					currentEpisode = episodesList.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);

					if (currentEpisode == null)
					{
						Console.WriteLine($"[WARNING] Episode {episodeNumber} not found, using first episode");
						currentEpisode = episodesList.First();
					}

					Console.WriteLine($"[DEBUG] Current episode: #{currentEpisode.EpisodeNumber} - {currentEpisode.Title}");

					// Get media file for episode
					var episodeMediaFiles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
					mediaFile = episodeMediaFiles.FirstOrDefault(m => m.FileType == "video");

					Console.WriteLine($"[DEBUG] Media files for episode: {episodeMediaFiles.Count()}");

					ViewBag.CurrentEpisode = currentEpisode;
					ViewBag.Episodes = episodesList;
				}
				else
				{
					Console.WriteLine("[INFO] Processing Movie type");

					// Get media file for movie
					var movieMediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
					mediaFile = movieMediaFiles.FirstOrDefault(m => m.FileType == "video");

					Console.WriteLine($"[DEBUG] Media files for movie: {movieMediaFiles.Count()}");
				}

				if (mediaFile == null)
				{
					Console.WriteLine("[ERROR] No video file found!");
					TempData["Error"] = "Video chưa được upload! Vui lòng liên hệ Admin.";
					return RedirectToAction("Detail", new { slug });
				}

				Console.WriteLine($"[SUCCESS] Media file found: {mediaFile.FileName}");
				Console.WriteLine($"  - Type: {mediaFile.FileType}");
				Console.WriteLine($"  - URL: {mediaFile.FileUrl}");
				Console.WriteLine($"  - Quality: {mediaFile.Quality}");

				ViewBag.MediaFile = mediaFile;

				// ✅ Prepare view model
				var viewModel = new MovieDetailsViewModel
				{
					Movie = movie,
					Episodes = episodesList
				};

				Console.WriteLine("[SUCCESS] Returning Watch view");
				Console.WriteLine("========================================");

				return View(viewModel);
			}
			catch (Exception ex)
			{
				Console.WriteLine("========================================");
				Console.WriteLine($"[EXCEPTION] Error in Watch action:");
				Console.WriteLine($"  Message: {ex.Message}");
				Console.WriteLine($"  StackTrace: {ex.StackTrace}");
				Console.WriteLine("========================================");

				TempData["Error"] = $"Có lỗi xảy ra: {ex.Message}";
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