using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.API.Controllers
{
	public class EpisodesController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
