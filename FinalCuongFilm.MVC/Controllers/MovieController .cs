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

		// GET: /Movies/Watch/{slug}?ep={episodeNumber} - Đã hợp nhất từ MovieController
		// GET: /Movie/Watch/{slug}?ep=1
		[AllowAnonymous]
		public async Task<IActionResult> Watch(string slug, int? ep = null)
		{
			var movie = await _movieService.GetBySlugAsync(slug);
			if (movie == null)
			{
				return NotFound();
			}

			// Increment view count
			await _movieService.IncrementViewCountAsync(movie.Id);

			FinalCuongFilm.Common.DTOs.MediaFileDto? mediaFile = null;
			FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;

			if (movie.Type == FinalCuongFilm.ApplicationCore.Entities.Enum.MovieType.Series)
			{
				// Get episodes
				var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);

				if (!episodes.Any())
				{
					TempData["Error"] = "Phim chưa có tập nào!";
					return RedirectToAction("Detail", new { slug });
				}

				// Get current episode
				var episodeNumber = ep ?? 1;
				currentEpisode = episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber);

				if (currentEpisode == null)
				{
					currentEpisode = episodes.OrderBy(e => e.EpisodeNumber).First();
				}

				// Get media file for episode
				var mediaFiles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
				mediaFile = mediaFiles.FirstOrDefault(m => m.FileType == "video");

				ViewBag.Episodes = episodes;
				ViewBag.CurrentEpisode = currentEpisode;
			}
			else
			{
				// Movie type - get media file
				var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
				mediaFile = mediaFiles.FirstOrDefault(m => m.FileType == "video");
			}

			if (mediaFile == null)
			{
				TempData["Error"] = "Video chưa được upload!";
				return RedirectToAction("Detail", new { slug });
			}

			ViewBag.Movie = movie;
			ViewBag.MediaFile = mediaFile;

			return View();
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