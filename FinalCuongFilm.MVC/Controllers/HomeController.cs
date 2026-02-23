using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.MVC.Models;
using FinalCuongFilm.Service.Interfaces;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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

		public async Task<IActionResult> Index()
		{
			if (User.IsInRole("Admin"))
			{
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
			}


			var allMovies = await _movieService.GetAllAsync();

			var latestMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ReleaseYear)
				.Take(12)
				.ToList();
			foreach (var movie in latestMovies)
			{
				_logger.LogInformation($"Movie: {movie.Title}, Slug: '{movie.Slug}'");
			}
			var popularMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ViewCount)
				.Take(12)
				.ToList();

			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.LatestMovies = latestMovies;
			ViewBag.PopularMovies = popularMovies;
			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			return View();
		}

		// Profile
		[Authorize]
		public IActionResult Profile()
		{
			return View();
		}

		// Continue Watching
		[Authorize]
		public IActionResult ContinueWatching()
		{
			return View();
		}

		//  My List - Hiển thị favorites
		[Authorize]
		public async Task<IActionResult> MyList()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null)
			{
				return RedirectToPage("/Account/Login", new { area = "Identity" });
			}

			var favorites = await _favoriteService.GetUserFavoritesAsync(userId);

			ViewData["Title"] = "Danh sách của tôi";
			return View(favorites);
		}

		public IActionResult Privacy()
		{
			return View();
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}