using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class GenresController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
