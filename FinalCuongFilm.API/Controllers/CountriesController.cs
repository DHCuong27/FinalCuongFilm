using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class CountriesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
