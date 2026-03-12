using FinalCuongFilm.Datalayer;
using FinalCuongFilm.DataLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly CuongFilmDbContext _context;

		public AdminController(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			// Thống kê
			ViewBag.TotalMovies = await _context.Movies.CountAsync();
			ViewBag.TotalActors = await _context.Actors.CountAsync();
			ViewBag.TotalGenres = await _context.Genres.CountAsync();
			ViewBag.TotalCountries = await _context.Countries.CountAsync();
			ViewBag.TotalLanguages = await _context.Languages.CountAsync();
			ViewBag.TotalEpisodes = await _context.Episodes.CountAsync();
			ViewBag.TotalMediaFiles = await _context.MediaFiles.CountAsync();
			//ViewBag.TotalUsers = await _context.Users.CountAsync();

			return View();
		}
	}
}