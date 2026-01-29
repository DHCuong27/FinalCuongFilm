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

		public HomeController(
			ILogger<HomeController> logger,
			IMovieService movieService,
			IGenreService genreService)
		{
			_logger = logger;
			_movieService = movieService;
			_genreService = genreService;
		}

		// GET: /
		public async Task<IActionResult> Index()
		{
			// Nếu user là Admin, redirect về Admin Dashboard
			if (User.IsInRole("Admin"))
			{
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
			}

			// Lấy phim mới nhất (Active)
			var allMovies = await _movieService.GetAllAsync();
			var activeMovies = allMovies
				.Where(m => m.IsActive)
				.OrderByDescending(m => m.ReleaseYear)
				.Take(12)
				.ToList();

			// Lấy thể loại để hiển thị menu
			var genres = await _genreService.GetAllAsync();
			ViewBag.Genres = genres;

			return View(activeMovies);
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