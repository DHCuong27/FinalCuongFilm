using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class ActorsController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
