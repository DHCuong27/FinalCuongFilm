using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.MVC.Models;
using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;
		private readonly IFavoriteService _favoriteService;
		private readonly IMemoryCache _cache;

		public HomeController(
			ILogger<HomeController> logger,
			IMovieService movieService,
			IGenreService genreService,
			ICountryService countryService,
			IMemoryCache cache,
			IFavoriteService favoriteService)
		{
			_logger = logger;
			_movieService = movieService;
			_genreService = genreService;
			_countryService = countryService;
			_favoriteService = favoriteService;
			_cache = cache;
		}

		public async Task<IActionResult> Index(
			string? search = null, Guid? genreId = null, Guid? countryId = null,
			int? releaseYear = null, int? type = null, string sortBy = "latest",
			int pageNumber = 1, int pageSize = 12)
		{
			if (User.IsInRole("Admin"))
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

			ViewData["Title"] = "CuongFilm - Xem phim chất lượng cao";
			ViewData["MetaDescription"] = "CuongFilm - Xem phim chất lượng cao, phim mới cập nhật mỗi ngày.";
			ViewData["CanonicalUrl"] = "https://cuongfilm.site/";

			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();
			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			// ==========================================
			// BƯỚC 1: CACHE CÁC SLIDER CỐ ĐỊNH (Tránh gọi Supabase liên tục)
			// ==========================================
			string cacheKey = "HomeFixedSlidersData";
			if (!_cache.TryGetValue(cacheKey, out HomeFilterViewModel homeVM))
			{
				var baseQuery = _movieService.GetBaseActiveMoviesQuery();

				// Thực thi thành 4 câu Query nhỏ, ép SQL tự tính toán và chỉ trả về 12 dòng DTO siêu nhẹ
				var latestMovies = await _movieService.MapToLightweightDto(
					baseQuery.OrderByDescending(m => m.CreatedAt).Take(12)).ToListAsync();

				var popularMovies = await _movieService.MapToLightweightDto(
					baseQuery.OrderByDescending(m => m.ViewCount).Take(12)).ToListAsync();

				var koreanMovies = await _movieService.MapToLightweightDto(
					baseQuery.Where(m => m.Country != null && (m.Country.Name == "South Korea" || m.Country.Name == "Hàn Quốc"))
							 .OrderByDescending(m => m.CreatedAt).Take(12)).ToListAsync();

				var chineseMovies = await _movieService.MapToLightweightDto(
					baseQuery.Where(m => m.Country != null && (m.Country.Name == "China" || m.Country.Name == "Trung Quốc"))
							 .OrderByDescending(m => m.CreatedAt).Take(12)).ToListAsync();

				// Khởi tạo Model để cache lại
				homeVM = new HomeFilterViewModel
				{
					LatestMovies = latestMovies,
					PopularMovies = popularMovies,
					KoreanMovies = koreanMovies,
					ChineseMovies = chineseMovies,
					ContinueWatchingMovies = new List<MovieDto>()
				};

				// Lưu vào RAM của Railway trong 10 phút
				var cacheEntryOptions = new MemoryCacheEntryOptions()
					.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
				_cache.Set(cacheKey, homeVM, cacheEntryOptions);
			}

			// ==========================================
			// BƯỚC 2: XỬ LÝ FILTER ĐỘNG (Push xuống SQL)
			// ==========================================
			var query = _movieService.GetBaseActiveMoviesQuery();

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(m => m.Title.ToLower().Contains(search.ToLower()));

			if (genreId.HasValue)
				query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));

			if (countryId.HasValue)
				query = query.Where(m => m.CountryId == countryId.Value);

			if (releaseYear.HasValue)
				query = query.Where(m => m.ReleaseYear == releaseYear.Value);

			if (type.HasValue)
				query = query.Where(m => (int)m.Type == type.Value);

			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"year_asc" => query.OrderBy(m => m.ReleaseYear),
				"year_desc" => query.OrderByDescending(m => m.ReleaseYear),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.CreatedAt) // Sửa lại order by CreatedAt cho chuẩn 'latest'
			};

			// Đếm tổng số lượng (Phục vụ phân trang)
			var totalItems = await query.CountAsync();

			// Cắt trang (Skip & Take) TẠI DATABASE, sau đó mới Map ra DTO nhẹ và kéo về RAM
			var pagedMovies = await _movieService.MapToLightweightDto(
				query.Skip((pageNumber - 1) * pageSize).Take(pageSize)
			).ToListAsync();

			// ==========================================
			// BƯỚC 3: GẮN DỮ LIỆU FILTER VÀO MODEL ĐÃ CACHE
			// ==========================================
			homeVM.AllMoviesFilter = new MovieFilterViewModel
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
				TotalItems = totalItems
			};

			return View(homeVM);
		}


		// GET: /Home/Browse?type=1
		public async Task<IActionResult> Browse(
			int type,
			string? search = null,
			Guid? genreId = null,
			Guid? countryId = null,
			string sortBy = "latest",
			int pageNumber = 1)
		{
			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();


			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			// Base filter by movie type (Movie or TV Series)
			var query = allMovies.Where(m => m.IsActive && (int)m.Type == type).AsEnumerable();

			// Apply optional filters
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

			// Sorting logic matching the UI dropdown
			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear) // Default "latest"
			};

			// Pagination
			int pageSize = 12;
			var filteredList = query.ToList();
			int totalItems = filteredList.Count;
			var pagedMovies = filteredList.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

			var viewModel = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Genres = genres,
				Countries = countries,
				Type = type,
				Search = search,
				GenreId = genreId,
				CountryId = countryId,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = type == 1 ? "Movies" : "TV Series",
				PageSubTitle = $"{totalItems} {(type == 2 ? "series" : "films")} found"
			};

			return View(viewModel);
		}
		// Privacy
		public IActionResult Privacy() => View();

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
			=> View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}
