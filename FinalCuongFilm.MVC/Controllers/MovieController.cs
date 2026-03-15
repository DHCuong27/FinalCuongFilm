using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
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
		private readonly IActorService _actorService;
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
			IActorService actorService,
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
			_actorService = actorService;
			_azureBlobService = azureBlobService;
			_logger = logger;
		}

		// GET: /Movie?search=&genreId=&countryId=&releaseYear=&type=&sortBy=&pageNumber=&pageSize=
		public async Task<IActionResult> Index(
			string? search = null,
			Guid? genreId = null,
			Guid? countryId = null,
			int? releaseYear = null,
			int? type = null,
			string sortBy = "latest",
			int pageNumber = 1,
			int pageSize = 12)
		{

			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			var query = allMovies.Where(m => m.IsActive).AsEnumerable();

			// ── Filter ──
			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(m =>
					m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					(m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

			if (genreId.HasValue)
				query = query.Where(m => m.SelectedGenreIds.Contains(genreId.Value));

			if (countryId.HasValue)
				query = query.Where(m => m.CountryId == countryId.Value);

			if (releaseYear.HasValue)
				query = query.Where(m => m.ReleaseYear == releaseYear.Value);

			if (type.HasValue)
				query = query.Where(m => (int)m.Type == type.Value);

			// ── Sort ──
			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"year_asc" => query.OrderBy(m => m.ReleaseYear),
				"year_desc" => query.OrderByDescending(m => m.ReleaseYear),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear)
			};

			// ── Pagination ──
			var filteredList = query.ToList();
			var totalItems = filteredList.Count;
			var pagedMovies = filteredList
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			var pageTitle = type switch
			{
				1 => "Movies",
				2 => "TV Series",
				_ => "All Films"
			};

			var vm = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Genres = genres,
				Countries = countries,
				Search = search,
				GenreId = genreId,
				CountryId = countryId,
				ReleaseYear = releaseYear,
				Type = type,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = pageTitle,
				PageSubTitle = $"{totalItems} films found"
			};

			return View(vm);
		}

		// GET: /Movie/Detail/{slug}
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

			// THÊM MỚI: Lấy danh sách diễn viên của phim
			// Giả sử bạn có _actorService và hàm GetActorsByMovieIdAsync trả về List<ActorDto>
			var actors = allMovies.FirstOrDefault(m => m.Id == movie.Id)?.SelectedActorIds != null
    ? await _actorService.GetAllAsync()
        .ContinueWith(task => task.Result.Where(a => movie.SelectedActorIds.Contains(a.Id)).ToList())
    : new List<FinalCuongFilm.Common.DTOs.ActorDto>();

			// Lấy genres và countries cho navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			var relatedMovies = allMovies
				.Where(m => m.IsActive &&
							m.Id != movie.Id &&
							(m.CountryId == movie.CountryId ||
							 m.SelectedGenreIds.Intersect(movie.SelectedGenreIds).Any()))
				.OrderByDescending(m => m.ViewCount)
				.Take(6)
				.ToList();

			ViewBag.Genres = genres; // Sửa lại một chút để tái sử dụng biến genres ở trên, tránh gọi DB 2 lần
			ViewBag.Countries = countries; // Tương tự với countries

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ViewBag.IsFavorited = false;
			if (userId != null)
				ViewBag.IsFavorited = await _favoriteService.IsFavoriteAsync(userId, movie.Id);

			try { ViewBag.Rating = await _reviewService.GetMovieRatingAsync(movie.Id); }
			catch { ViewBag.Rating = null; }

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
				RelatedMovies = relatedMovies,

				// THÊM MỚI: Gán danh sách diễn viên vào ViewModel
				Actors = actors.ToList()
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

				// Lấy genres và countries cho navigation
				ViewBag.Genres = await _genreService.GetAllAsync();
				ViewBag.Countries = await _countryService.GetAllAsync();

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

					var episodeMedia = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
					allQualityOptions = episodeMedia
						.Where(m => m.FileType == "video")
						.OrderByDescending(m => m.Quality)
						.ToList();

					selectedMediaFile = allQualityOptions.FirstOrDefault();
				}
				else
				{
					var movieMedia = await _mediaFileService.GetByMovieIdAsync(movie.Id);
					allQualityOptions = movieMedia
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

				var streamingUrl = await _azureBlobService.GetStreamingUrlAsync(selectedMediaFile.FileUrl, expiryHours: 4);
				var qualitySources = new List<object>();
				foreach (var qf in allQualityOptions)
				{
					var qUrl = await _azureBlobService.GetStreamingUrlAsync(qf.FileUrl, expiryHours: 4);
					qualitySources.Add(new { id = qf.Id, quality = qf.Quality ?? "Auto", url = qUrl });
				}

				var subtitleFiles = new List<object>();
				var allMediaForSub = currentEpisode != null
					? await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id)
					: await _mediaFileService.GetByMovieIdAsync(movie.Id);

				foreach (var sub in allMediaForSub.Where(m => m.FileType == "subtitle"))
				{
					var subUrl = await _azureBlobService.GetStreamingUrlAsync(sub.FileUrl, expiryHours: 4);
					subtitleFiles.Add(new { language = sub.Language ?? "Unknown", url = subUrl, label = sub.Language ?? "Subtitle" });
				}

				await _movieService.IncrementViewCountAsync(movie.Id);

				// THÊM MỚI 1: Khởi tạo MovieWatchViewModel
				var viewModel = new FinalCuongFilm.MVC.Models.ViewModels.MovieWatchViewModel
				{
					Movie = movie,
					Episodes = allEpisodes,
					CurrentEpisode = currentEpisode,
					MediaFiles = allQualityOptions
				};

				// THÊM MỚI 2: Lấy trạng thái Yêu thích (Favorite)
				var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
				bool isFavorited = false;
				if (userId != null)
				{
					isFavorited = await _favoriteService.IsFavoriteAsync(userId, movie.Id);
				}

				// THÊM MỚI 3: Lấy danh sách diễn viên (Giả sử bạn có _actorService)
				// Nếu chưa có service này, bạn hãy tạo hoặc cmt dòng này lại tạm thời nhé
				var allActors = await _actorService.GetAllAsync();
var actors = movie.SelectedActorIds != null
    ? allActors.Where(a => movie.SelectedActorIds.Contains(a.Id)).ToList()
    : new List<FinalCuongFilm.Common.DTOs.ActorDto>();

				// Gán các biến phụ trợ vào ViewBag
				ViewBag.StreamingUrl = streamingUrl;
				ViewBag.QualitySources = qualitySources;
				ViewBag.SubtitleFiles = subtitleFiles;
				ViewBag.MovieId = movie.Id;
				ViewBag.EpisodeId = currentEpisode?.Id;
				ViewBag.IsFavorited = isFavorited;
				ViewBag.Actors = actors;

				_logger.LogInformation($"[Watch] Success: {movie.Title}, quality options: {qualitySources.Count}");

				// THÊM MỚI 4: Truyền viewModel vào View thay vì để trống
				return View(viewModel);
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
			if (movie == null || !movie.IsActive) return NotFound();
			return RedirectToAction("Watch", new { slug = movie.Slug });
		}
	}
}