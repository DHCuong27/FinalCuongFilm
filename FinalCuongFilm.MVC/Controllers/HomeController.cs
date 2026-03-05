using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.MVC.Models;
using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

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

			var allMovies = await _movieService.GetAllAsync();
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			// ── Hero sections (không filter, không phân trang) ──
			var latestMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ReleaseYear)
				.Take(12)
				.ToList();

			var popularMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(12)
				.ToList();

			// ── Section "Tất Cả Phim" — có filter + phân trang ──
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
				_ => query.OrderByDescending(m => m.ReleaseYear) // "latest"
			};

			var filteredList = query.ToList();
			var totalItems = filteredList.Count;
			var pagedMovies = filteredList
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

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

			var homeVM = new HomeFilterViewModel
			{
				LatestMovies = latestMovies,
				PopularMovies = popularMovies,
				AllMoviesFilter = filterVM
			};

			return View(homeVM);
		}

		// Profile
		[Authorize]
		public IActionResult Profile() => View();

		// Continue Watching
		[Authorize]
		public IActionResult ContinueWatching() => View();

		// My List
		[Authorize]
		public async Task<IActionResult> MyList()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
			ViewData["Title"] = "Danh sách của tôi";
			return View(favorites);
		}

		public IActionResult Privacy() => View();

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
			=> View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
	}
}