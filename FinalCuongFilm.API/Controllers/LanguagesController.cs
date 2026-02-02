using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class LanguagesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
