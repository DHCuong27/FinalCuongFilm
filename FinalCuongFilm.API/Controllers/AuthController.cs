using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class AuthController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
