using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	[Authorize(Roles = "User")]
	public class UsersController : Controller
	{
		public IActionResult Index() => View();


		public IActionResult Profile() => View();

	}
}
