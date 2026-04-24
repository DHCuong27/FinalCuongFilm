using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.MVC.Models;
using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;
		private readonly IFavoriteService _favoriteService;

		public HomeController(
			ILogger<HomeController> logger,
			IMovieService movieService,
			IGenreService genreService,
			ICountryService countryService,
			IFavoriteService favoriteService)
		{
			_logger = logger;
			_movieService = movieService;
			_genreService = genreService;
			_countryService = countryService;
			_favoriteService = favoriteService;
		}

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
			if (User.IsInRole("Admin"))
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

			// 1. Lấy toàn bộ dữ liệu gốc
			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			// 2. Lấy các danh sách cố định cho Slider và các Section ngang
			var latestMovies = allMovies
				.Where(m => m.IsActive)
				.Take(12) // Lấy 12 phim thay vì 5 như comment cũ để Slider chạy mượt hơn
				.ToList();

			var popularMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(12)
				.ToList();

			// ĐÃ FIX: Lọc phim Hàn Quốc và Trung Quốc (Bắt cả tiếng Anh lẫn tiếng Việt cho chắc)
			var koreanMovies = allMovies
				.Where(m => m.IsActive && (m.CountryName == "South Korea" || m.CountryName == "Hàn Quốc"))
				.ToList();

			var chineseMovies = allMovies
				.Where(m => m.IsActive && (m.CountryName == "China" || m.CountryName == "Trung Quốc"))
				.ToList();

			// 3. Xử lý phần "Tất Cả Phim" có Filter + Phân trang
			var query = allMovies.Where(m => m.IsActive).AsEnumerable();

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
			var pagedMovies = filteredList
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			// 4. Đóng gói dữ liệu Filter
			var filterVM = new MovieFilterViewModel
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

			// 5. CHÌA KHÓA Ở ĐÂY: Gôm TẤT CẢ vào MỘT ViewModel duy nhất để gửi ra View
			var homeVM = new HomeFilterViewModel
			{
				LatestMovies = latestMovies,
				PopularMovies = popularMovies,
				KoreanMovies = koreanMovies,     // Data đã được nạp
				ChineseMovies = chineseMovies,   // Data đã được nạp
				ContinueWatchingMovies = new List<MovieDto>(), // Chờ tích hợp DB sau
				AllMoviesFilter = filterVM
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

			// Chuẩn bị dữ liệu cho Dropdown ngoài View
			ViewBag.Genres = await _genreService.GetAllAsync();
			ViewBag.Countries = await _countryService.GetAllAsync();

			// 1. Lọc cốt lõi theo Type (Movie / TV Series)
			var query = allMovies.Where(m => m.IsActive && (int)m.Type == type).AsEnumerable();

			// 2. CÁC BỘ LỌC TÙY CHỌN (FILTER)
			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(m => m.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

			if (genreId.HasValue)
				query = query.Where(m => m.SelectedGenreIds.Contains(genreId.Value));

			if (countryId.HasValue)
				query = query.Where(m => m.CountryId == countryId.Value);

			// 3. Sắp xếp
			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"year_asc" => query.OrderBy(m => m.ReleaseYear),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear) // "latest"
			};

			// 4. Phân trang
			int totalItems = query.Count();
			int pageSize = 12;
			var pagedMovies = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

			// Khởi tạo ViewModel (Đảm bảo VM của bạn có thêm mấy thuộc tính Search, GenreId... để lưu trạng thái)
			var viewModel = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Type = type,

				// Lưu lại trạng thái Filter để View hiển thị
				Search = search,
				GenreId = genreId,
				CountryId = countryId,
				SortBy = sortBy,

				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = type == 1 ? "Movies" : "TV Series"
				//PageSubTitle = type == 0 ? "Khám phá các bộ phim chiếu rạp đặc sắc nhất" : "Cày xuyên đêm với các series phim bộ đỉnh cao"
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