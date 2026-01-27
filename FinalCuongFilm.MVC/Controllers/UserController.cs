using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	[Authorize(Roles = "User")]
	public class UsersController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Profile()
		{
			return View();
		}
	}
}
