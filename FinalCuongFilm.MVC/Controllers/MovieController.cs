
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
		private readonly IStorageService _storageService;
		private readonly IVipService _vipService;
		private readonly ILogger<MovieController> _logger;
		private readonly CuongFilmDbContext _context;
		private readonly IConfiguration _configuration;
		private readonly IWebHostEnvironment _env;

		public MovieController(
			IMovieService movieService, IFavoriteService favoriteService,
			IReviewService reviewService, IEpisodeService episodeService,
			IMediaFileService mediaFileService, IGenreService genreService,
			ICountryService countryService, IActorService actorService,
			IVipService vipService, IStorageService storageService, CuongFilmDbContext context, IConfiguration configuration, IWebHostEnvironment env, ILogger<MovieController> logger)
		{
			_movieService = movieService; _favoriteService = favoriteService;
			_reviewService = reviewService; _episodeService = episodeService;
			_mediaFileService = mediaFileService; _genreService = genreService;
			_countryService = countryService; _actorService = actorService;
			_vipService = vipService; _storageService = storageService; _context = context; _configuration = configuration; _env = env; _logger = logger;
		}

		// GET: /Movie
		public async Task<IActionResult> Index(
			string? search = null, Guid? genreId = null, Guid? countryId = null,
			int? type = null, string sortBy = "latest",
			int pageNumber = 1, int pageSize = 12)
		{
			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			var query = allMovies.Where(m => m.IsActive).AsEnumerable();

			// 1. Filter by Search Keyword
			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(m => m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
										(m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			// 2. Filter by Genre (Added null check for safety)
			if (genreId.HasValue)
			{
				query = query.Where(m => m.SelectedGenreIds != null && m.SelectedGenreIds.Contains(genreId.Value));
			}

			// 3. Filter by Country
			if (countryId.HasValue)
			{
				query = query.Where(m => m.CountryId == countryId.Value);
			}

			// 4. Filter by Movie Type (1 = Movie, 2 = TV Series)
			if (type.HasValue)
			{
				query = query.Where(m => (int)m.Type == type.Value);
			}

			// 5. Sorting Logic (Strictly matched to the UI options)
			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear) // Default is "latest"
			};

			// 6. Pagination
			var filteredList = query.ToList();
			var totalItems = filteredList.Count;
			var pagedMovies = filteredList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

			// 7. Prepare ViewModel
			var vm = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Genres = genres,
				Countries = countries,
				Search = search,
				GenreId = genreId,
				CountryId = countryId,
				Type = type,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = type switch { 1 => "Movies", 2 => "TV Series", _ => "All Movies" },
				PageSubTitle = $"{totalItems} movies found"
			};
			ViewData["Title"] = "CuongFilm";
			ViewData["MetaDescription"] = "CuongFilm - Xem phim mới, phim lẻ, phim bộ chất lượng cao, cập nhật mỗi ngày.";
			ViewData["CanonicalUrl"] = "https://cuongfilm.site/";
			return View(vm);
		}

		// Details: /Movie/Detail/{slug}
		public async Task<IActionResult> Detail(string slug)
		{
			if (string.IsNullOrEmpty(slug)) return NotFound();

			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);
			if (movie == null) return NotFound();

			ViewData["Title"] = movie.Title;
			ViewData["MetaDescription"] = movie.Description;
			ViewData["OgImage"] = movie.PosterUrl;
			ViewData["OgType"] = "video.movie";
			ViewData["CanonicalUrl"] = $"https://cuongfilm.site/movie/{movie.Slug}";
			ViewBag.Genres = await _genreService.GetAllAsync();
			ViewBag.Countries = await _countryService.GetAllAsync();

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

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			ViewBag.IsFavorited = userId != null && await _favoriteService.IsFavoriteAsync(userId, movie.Id);

			ViewBag.Rating = await _reviewService.GetMovieRatingAsync(movie.Id);

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
			ViewBag.Reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: false);
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
					if (string.IsNullOrEmpty(userId))
					{
						TempData["Warning"] = "Đây là bộ phim Premium. Vui lòng đăng nhập để tiếp tục!";
						return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = Request.Path });
					}

					bool hasVip = await _vipService.HasActiveVipAsync(userId);
					if (!hasVip)
					{
						TempData["Warning"] = "Phim này dành riêng cho tài khoản Premium. Vui lòng nâng cấp gói để xem!";
						return RedirectToAction("Index", "Premium");
					}
				}

				// 2. Xử lý logic Tập phim & Lấy danh sách File Media
				var allEpisodes = new List<FinalCuongFilm.Common.DTOs.EpisodeDto>();
				FinalCuongFilm.Common.DTOs.EpisodeDto? currentEpisode = null;
				List<FinalCuongFilm.Common.DTOs.MediaFileDto> mediaFilesList = new();

				if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series)
				{
					var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
					allEpisodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList();

					if (!allEpisodes.Any())
					{
						TempData["Warning"] = "Chưa có tập phim nào.";
						return RedirectToAction("Detail", new { slug });
					}

					int targetEp = ep ?? 1;
					currentEpisode = allEpisodes.FirstOrDefault(e => e.EpisodeNumber == targetEp) ?? allEpisodes.First();

					var byEpisode = await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id)
						?? Enumerable.Empty<FinalCuongFilm.Common.DTOs.MediaFileDto>();
					var byMovie = await _mediaFileService.GetByMovieIdAsync(movie.Id)
						?? Enumerable.Empty<FinalCuongFilm.Common.DTOs.MediaFileDto>();

					mediaFilesList = byEpisode.Concat(byMovie)
						.Where(m => m != null)
						.GroupBy(m => m.Id)
						.Select(g => g.First())
						.ToList();
				}
				else
				{
					var byMovie = await _mediaFileService.GetByMovieIdAsync(movie.Id)
						?? Enumerable.Empty<FinalCuongFilm.Common.DTOs.MediaFileDto>();

					mediaFilesList = byMovie.Where(m => m != null).ToList();
				}

				// ĐÃ XÓA CHECK !mediaFilesList.Any() Ở ĐÂY ĐỂ MỞ ĐƯỜNG CHO FALLBACK CHẠY!

				// 3. Tối ưu hóa xử lý Streaming URL (HLS vs MP4) - BỎ SAS TOKEN
				string? streamingUrl = null;
				var qualitySources = new List<object>();

				bool IsHls(FinalCuongFilm.Common.DTOs.MediaFileDto m) =>
					(!string.IsNullOrWhiteSpace(m.FileType) && m.FileType.Trim().ToLower().Contains("hls")) ||
					(!string.IsNullOrWhiteSpace(m.FileUrl) && m.FileUrl.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase));

				bool IsMp4(FinalCuongFilm.Common.DTOs.MediaFileDto m) =>
					(!string.IsNullOrWhiteSpace(m.FileType) && m.FileType.Trim().ToLower().Contains("video")) ||
					(!string.IsNullOrWhiteSpace(m.FileUrl) && m.FileUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

				var hlsFile = mediaFilesList.FirstOrDefault(m => IsHls(m));

				if (hlsFile != null && !string.IsNullOrWhiteSpace(hlsFile.FileUrl))
				{
					// Supabase Public Bucket -> Dùng thẳng URL gốc không cần GetStreamingUrlAsync
					streamingUrl = hlsFile.FileUrl;
					ViewBag.MediaType = "hls";
					qualitySources.Add(new { id = hlsFile.Id, quality = "Auto (HLS)", url = streamingUrl });
				}
				else
				{
					var videoFiles = mediaFilesList
						.Where(m => IsMp4(m) && !string.IsNullOrWhiteSpace(m.FileUrl))
						.OrderByDescending(m => m.Quality)
						.ToList();

					if (videoFiles.Any())
					{
						ViewBag.MediaType = "mp4";
						foreach (var qf in videoFiles)
						{
							// Dùng thẳng URL gốc
							qualitySources.Add(new { id = qf.Id, quality = qf.Quality ?? "HD", url = qf.FileUrl });
						}
						streamingUrl = videoFiles.FirstOrDefault()?.FileUrl;
					}
				}

				// ✅ FALLBACK: Xử lý thông minh cho cả Phim lẻ và Phim bộ nếu DB chưa có File
				if (string.IsNullOrWhiteSpace(streamingUrl))
				{
					var supabaseUrl = _configuration["SUPABASE_URL"];
					if (!string.IsNullOrWhiteSpace(supabaseUrl))
					{
						string fallbackUrl = "";

						if (movie.Type == ApplicationCore.Entities.Enum.MovieType.Series && currentEpisode != null)
						{
							fallbackUrl = $"{supabaseUrl}/storage/v1/object/public/videos/movies/{movie.Slug}/ep{currentEpisode.EpisodeNumber}/hls/master.m3u8";
						}
						else
						{
							fallbackUrl = $"{supabaseUrl}/storage/v1/object/public/videos/movies/{movie.Slug}/hls/master.m3u8";
						}

						streamingUrl = fallbackUrl;
						ViewBag.MediaType = "hls";
						qualitySources.Add(new { id = Guid.Empty, quality = "Auto (HLS)", url = streamingUrl });
						_logger.LogWarning("[Watch] Fallback HLS URL used: {Url}", fallbackUrl);
					}
				}

				if (string.IsNullOrWhiteSpace(streamingUrl))
				{
					TempData["Warning"] = "Video chưa sẵn sàng hoặc không tìm thấy file HLS/MP4 hợp lệ.";
					return RedirectToAction("Detail", new { slug });
				}

				// 4. Subtitles - BỎ SAS TOKEN
				var subtitleFilesList = mediaFilesList.Where(m => m.FileType?.ToLower() == "subtitle").ToList();
				var subtitleFiles = subtitleFilesList.Select(sub => new {
					language = sub.Language ?? "en",
					url = sub.FileUrl, // Dùng thẳng URL
					label = sub.Language ?? "Subtitle"
				}).ToList();

				// 5. Update view & History
				await _movieService.IncrementViewCountAsync(movie.Id);

				var actors = new List<FinalCuongFilm.Common.DTOs.ActorDto>();
				if (movie.SelectedActorIds != null && movie.SelectedActorIds.Any())
				{
					var allActors = await _actorService.GetAllAsync();
					actors = allActors.Where(a => movie.SelectedActorIds.Contains(a.Id)).ToList();
				}

				if (userId != null)
				{
					try { await _favoriteService.SaveWatchHistoryAsync(userId, movie.Id); }
					catch (Exception ex) { _logger.LogWarning(ex, "Không thể lưu lịch sử xem phim cho User {UserId}, Movie {MovieId}", userId, movie.Id); }
				}

				ViewBag.Genres = await _genreService.GetAllAsync();
				ViewBag.Countries = await _countryService.GetAllAsync();
				ViewBag.IsFavorited = userId != null && await _favoriteService.IsFavoriteAsync(userId, movie.Id);
				ViewBag.StreamingUrl = streamingUrl;
				ViewBag.QualitySources = qualitySources;
				ViewBag.SubtitleFiles = subtitleFiles;
				ViewBag.Actors = actors;
				ViewBag.Reviews = await _reviewService.GetMovieReviewsAsync(movie.Id, approvedOnly: false);

				var viewModel = new MovieWatchViewModel
				{
					Movie = movie,
					Episodes = allEpisodes,
					CurrentEpisode = currentEpisode,
					MediaFiles = mediaFilesList
				};

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Watch] Error when load film: {Slug}", slug);
				TempData["Warning"] = "Có lỗi xảy ra khi tải phim. Vui lòng thử lại.";
				return RedirectToAction("Detail", new { slug });
			}
		}

		// Watch by ID: /Movie/WatchById/{id}
		public async Task<IActionResult> WatchById(Guid id)
		{
			var movie = await _movieService.GetByIdAsync(id);
			if (movie == null || !movie.IsActive) return NotFound();
			return RedirectToAction("Watch", new { slug = movie.Slug });
		}

		// GET: /Movie/PremiumMovies
		public async Task<IActionResult> PremiumMovies(
			string? search = null, Guid? genreId = null, Guid? countryId = null,
			int? type = null, string sortBy = "latest", int pageNumber = 1, int pageSize = 12)
		{
			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			var query = allMovies.Where(m => m.IsActive && m.IsVipOnly).AsEnumerable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(m =>
					m.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
					(m.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
			}

			if (genreId.HasValue)
			{
				query = query.Where(m => m.SelectedGenreIds != null && m.SelectedGenreIds.Contains(genreId.Value));
			}

			if (countryId.HasValue)
			{
				query = query.Where(m => m.CountryId == countryId.Value);
			}

			if (type.HasValue)
			{
				query = query.Where(m => (int)m.Type == type.Value);
			}

			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
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
				Type = type,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = "VIP Premium Movies",
				PageSubTitle = "Exclusive films for VIP members"
			};

			return View("Index", vm);
		}

		[Authorize]
		public async Task<IActionResult> Download(Guid id)
		{
			var movie = await _context.Movies.FindAsync(id);
			if (movie == null) return NotFound();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			bool hasVip = await _vipService.HasActiveVipAsync(userId);

			if (!hasVip)
			{
				TempData["Error"] = "The download feature is exclusive to Premium members. Please upgrade your account!";
				return RedirectToAction("Index", "Premium");
			}

			try
			{
				var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);
				var mp4 = mediaFiles
					.Where(m => m.FileType?.ToLower() == "video" && m.FileUrl.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
					.OrderByDescending(m => m.Quality)
					.FirstOrDefault();

				if (mp4 == null)
				{
					TempData["Error"] = "No MP4 file found for this movie.";
					return RedirectToAction("Detail", new { slug = movie.Slug });
				}

				return Redirect(mp4.FileUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing download for movie {MovieId}", id);
				TempData["Error"] = "Storage connection error: " + ex.Message;
				return RedirectToAction("Detail", new { slug = movie.Slug });
			}
		}

		private string GetSecureDownloadLink(string blobPath, string bucket = "videos")
		{
			if (string.IsNullOrWhiteSpace(blobPath)) return null;

			return $"{_configuration["SUPABASE_URL"]}/storage/v1/object/public/{bucket}/{blobPath}";
		}
	}
}