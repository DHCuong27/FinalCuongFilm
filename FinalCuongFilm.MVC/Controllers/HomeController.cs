using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.MVC.Models;
using FinalCuongFilm.Service.Interfaces;
using System.Diagnostics;

namespace FinalCuongFilm.MVC.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;

		public HomeController(
			ILogger<HomeController> logger,
			IMovieService movieService,
			IGenreService genreService,
			ICountryService countryService)
		{
			_logger = logger;
			_movieService = movieService;
			_genreService = genreService;
			_countryService = countryService;
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
		public IActionResult Profile()
		{
			if (!User.Identity.IsAuthenticated)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			return View();
		}

		// Continue Watching
		public IActionResult ContinueWatching()
		{
			if (!User.Identity.IsAuthenticated)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			return View();
		}

		// My List
		public IActionResult MyList()
		{
			if (!User.Identity.IsAuthenticated)
				return RedirectToPage("/Account/Login", new { area = "Identity" });

			return View();
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