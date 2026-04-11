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
		private readonly IVipService _vipService;
		private readonly ILogger<MovieController> _logger;

		public MovieController(
			IMovieService movieService, IFavoriteService favoriteService,
			IReviewService reviewService, IEpisodeService episodeService,
			IMediaFileService mediaFileService, IGenreService genreService,
			ICountryService countryService, IActorService actorService,
			IVipService vipService,IAzureBlobService azureBlobService, ILogger<MovieController> logger)
		{
			_movieService = movieService; _favoriteService = favoriteService;
			_reviewService = reviewService; _episodeService = episodeService;
			_mediaFileService = mediaFileService; _genreService = genreService;
			_countryService = countryService; _actorService = actorService;
			_vipService = vipService; _azureBlobService = azureBlobService; _logger = logger;
		}

		// GET: /Movie
		public async Task<IActionResult> Index(
			string? search = null, Guid? genreId = null, Guid? countryId = null,
			int? releaseYear = null, int? type = null, string sortBy = "latest",
			int pageNumber = 1, int pageSize = 12)
		{
			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			var query = allMovies.Where(m => m.IsActive).AsEnumerable();

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(m => m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
										(m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

			if (genreId.HasValue) query = query.Where(m => m.SelectedGenreIds.Contains(genreId.Value));
			if (countryId.HasValue) query = query.Where(m => m.CountryId == countryId.Value);
			if (releaseYear.HasValue) query = query.Where(m => m.ReleaseYear == releaseYear.Value);
			if (type.HasValue) query = query.Where(m => (int)m.Type == type.Value);

			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"year_asc" => query.OrderBy(m => m.ReleaseYear),
				"year_desc" => query.OrderByDescending(m => m.ReleaseYear),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear)
			};

			var filteredList = query.ToList();
			var totalItems = filteredList.Count;
			var pagedMovies = filteredList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

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
				PageTitle = type switch { 1 => "Movies", 2 => "TV Series", _ => "All Films" },
				PageSubTitle = $"{totalItems} films found"
			};

			return View(vm);
		}

		// Details: /Movie/Detail/{slug}
		public async Task<IActionResult> Detail(string slug)
		{
			if (string.IsNullOrEmpty(slug)) return NotFound();

			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);
			if (movie == null) return NotFound();

			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

			var actors = new List<FinalCuongFilm.Common.DTOs.ActorDto>();
			if (movie.SelectedActorIds != null && movie.SelectedActorIds.Any())
			{
				var allActors = await _actorService.GetAllAsync();
				actors = allActors.Where(a => movie.SelectedActorIds.Contains(a.Id)).ToList();
			}

			var relatedMovies = allMovies
				.Where(m => m.IsActive && m.Id != movie.Id &&
							(m.CountryId == movie.CountryId || m.SelectedGenreIds.Intersect(movie.SelectedGenreIds).Any()))
				.OrderByDescending(m => m.ViewCount)
				.Take(6).ToList();

			ViewBag.Genres = await _genreService.GetAllAsync();
			ViewBag.Countries = await _countryService.GetAllAsync();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ViewBag.IsFavorited = userId != null && await _favoriteService.IsFavoriteAsync(userId, movie.Id);

			try { ViewBag.Rating = await _reviewService.GetMovieRatingAsync(movie.Id); }
			catch { ViewBag.Rating = null; }

			var reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: true);
			ViewBag.Reviews = reviews.Take(5);
			ViewBag.UserReview = userId != null ? await _reviewService.GetUserReviewForMovieAsync(userId, movie.Id) : null;

			var viewModel = new MovieDetailsViewModel
			{
				Movie = movie,
				Episodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList(),
				MediaFiles = mediaFiles.ToList(),
				RelatedMovies = relatedMovies,
				Actors = actors
			};

			return View(viewModel);
		}


		// Watch: /Movie/Watch/{slug}?ep=1
		[AllowAnonymous]
		[Route("Movie/Watch/{slug}")]
		public async Task<IActionResult> Watch(string slug, int? ep = null)
		{
			if (string.IsNullOrEmpty(slug)) return RedirectToAction("Index", "Home");

			try
			{
				// 1. Lấy thông tin phim
				var allMovies = await _movieService.GetAllAsync();
				var movie = allMovies.FirstOrDefault(m => string.Equals(m.Slug, slug, StringComparison.OrdinalIgnoreCase) && m.IsActive);
				if (movie == null) return RedirectToAction("Index", "Home");

				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

				if (movie.IsVipOnly)
				{
					// Check 1: Chưa đăng nhập -> Đuổi ra trang Login
					if (string.IsNullOrEmpty(userId))
					{
						TempData["Warning"] = "Đây là bộ phim Premium. Vui lòng đăng nhập để tiếp tục!";
						return RedirectToAction("Login", "Auth", new { returnUrl = Request.Path });
					}

					// Check 2: Đã đăng nhập nhưng kiểm tra xem có VIP không?
					// Lưu ý: Bạn cần Inject IVipService vào MovieController để dùng được hàm này
					bool hasVip = await _vipService.HasActiveVipAsync(userId);

					if (!hasVip)
					{
						TempData["Warning"] = "Phim này dành riêng cho tài khoản Premium. Vui lòng nâng cấp gói để xem!";
						return RedirectToAction("Index", "Premium"); // Đuổi sang trang Mua VIP
					}
				}


				// 2. Xử lý logic Tập phim & Lấy danh sách File Media
				var allEpisodes = new List<FinalCuongFilm.Common.DTOs.EpisodeDto>();
				FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;
				IEnumerable<FinalCuongFilm.Common.DTOs.MediaFileDto> mediaFiles;

				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
				{
					var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
					allEpisodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList();

					if (!allEpisodes.Any()) return RedirectToAction("Detail", new { slug });

					int targetEp = ep ?? 1;
					currentEpisode = allEpisodes.FirstOrDefault(e => e.EpisodeNumber == targetEp) ?? allEpisodes.First();
					mediaFiles = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id);
				}
				else
				{
					mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
				}

				// 3. Tối ưu hóa xử lý Streaming URL (HLS vs MP4)
				string? streamingUrl = null;
				var qualitySources = new List<object>();
				var hlsFile = mediaFiles.FirstOrDefault(m => m.FileType?.ToLower() == "hls");

				if (hlsFile != null)
				{
					streamingUrl = await _azureBlobService.GetStreamingUrlAsync(hlsFile.FileUrl, expiryHours: 12);
					ViewBag.MediaType = "hls";
					qualitySources.Add(new { id = hlsFile.Id, quality = "Auto (HLS)", url = streamingUrl });
				}
				else
				{
					// Dự phòng: Lấy các file MP4 thường
					var videoFiles = mediaFiles.Where(m => m.FileType?.ToLower() == "video").OrderByDescending(m => m.Quality).ToList();
					if (videoFiles.Any())
					{
						ViewBag.MediaType = "mp4";

						// TỐI ƯU: Xin SAS Token từ Azure ĐỒNG THỜI cho tất cả các độ phân giải
						var sasTasks = videoFiles.Select(async qf =>
						{
							var qUrl = await _azureBlobService.GetStreamingUrlAsync(qf.FileUrl, expiryHours: 4);
							return new { id = qf.Id, quality = qf.Quality ?? "HD", url = qUrl };
						});

						var resolvedSources = await Task.WhenAll(sasTasks);
						qualitySources.AddRange(resolvedSources);

						// Lấy link có chất lượng cao nhất làm mặc định
						streamingUrl = resolvedSources.First().url;
					}
				}

				if (string.IsNullOrEmpty(streamingUrl)) return RedirectToAction("Detail", new { slug });

				// 4. TỐI ƯU: Xử lý Phụ đề (Subtitles) đồng thời
				var subtitleFilesList = mediaFiles.Where(m => m.FileType?.ToLower() == "subtitle").ToList();
				var subtitleTasks = subtitleFilesList.Select(async sub =>
				{
					var subUrl = await _azureBlobService.GetStreamingUrlAsync(sub.FileUrl, expiryHours: 4);
					return new { language = sub.Language ?? "en", url = subUrl, label = sub.Language ?? "Subtitle" };
				});
				var subtitleFiles = await Task.WhenAll(subtitleTasks);

				// 5. Tăng lượt xem & Lấy dữ liệu râu ria
				await _movieService.IncrementViewCountAsync(movie.Id);

				var actors = new List<FinalCuongFilm.Common.DTOs.ActorDto>();
				if (movie.SelectedActorIds != null && movie.SelectedActorIds.Any())
				{
					var allActors = await _actorService.GetAllAsync();
					actors = allActors.Where(a => movie.SelectedActorIds.Contains(a.Id)).ToList();
				}
						
				if (userId != null)
				{
					try
					{

						await _favoriteService.SaveWatchHistoryAsync(userId, movie.Id);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Không thể lưu lịch sử xem phim cho User {UserId}, Movie {MovieId}", userId, movie.Id);
					}
				}

				// Đổ dữ liệu đồng loạt ra ViewBag để tối ưu thời gian chờ
				ViewBag.Genres = await _genreService.GetAllAsync();
				ViewBag.Countries = await _countryService.GetAllAsync();
				ViewBag.IsFavorited = userId != null && await _favoriteService.IsFavoriteAsync(userId, movie.Id);
				ViewBag.StreamingUrl = streamingUrl;
				ViewBag.QualitySources = qualitySources;
				ViewBag.SubtitleFiles = subtitleFiles;
				ViewBag.Actors = actors;

				var viewModel = new MovieWatchViewModel
				{
					Movie = movie,
					Episodes = allEpisodes,
					CurrentEpisode = currentEpisode,
					MediaFiles = mediaFiles.ToList()
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Watch] Lỗi nghiêm trọng khi load phim: {Slug}", slug);
				return RedirectToAction("Index", "Home");
			}
		}
		// Watch by ID: /Movie/WatchById/{id}
		public async Task<IActionResult> WatchById(Guid id)
		{
			var movie = await _movieService.GetByIdAsync(id);
			if (movie == null || !movie.IsActive) return NotFound();
			return RedirectToAction("Watch", new { slug = movie.Slug });
		}
	}
}