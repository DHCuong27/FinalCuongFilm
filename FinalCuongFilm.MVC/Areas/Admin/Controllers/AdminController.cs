using FinalCuongFilm.ApplicationCore.Entities.Identity;
using FinalCuongFilm.DataLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.MVC.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly CuongFilmDbContext _context;
		private readonly UserManager<CuongFilmUser> _userManager;
		

		public AdminController(CuongFilmDbContext context, UserManager<CuongFilmUser> userManager)
		{
			_userManager = userManager;
			_context = context;
		}

		public async Task<IActionResult> Index()
		{

			ViewBag.TotalMovies = await _context.Movies.CountAsync();
			ViewBag.TotalActors = await _context.Actors.CountAsync();
			ViewBag.TotalGenres = await _context.Genres.CountAsync();
			ViewBag.TotalCountries = await _context.Countries.CountAsync();
			ViewBag.TotalLanguages = await _context.Languages.CountAsync();
			ViewBag.TotalEpisodes = await _context.Episodes.CountAsync();
			ViewBag.TotalMediaFiles = await _context.MediaFiles.CountAsync();
			ViewBag.TotalUsers = await _userManager.Users.CountAsync();
			ViewBag.TotalVipPackages = await _context.VipPackages.CountAsync();
			ViewBag.TotalUserSubscriptions = await _context.UserSubscriptions.CountAsync();
			ViewBag.TotalTransactions = await _context.Transactions.CountAsync();
			

			return View();
		}
	}
}