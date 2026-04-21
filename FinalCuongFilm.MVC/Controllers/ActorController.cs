using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	public class ActorController : Controller
	{
		private readonly IActorService _actorService;

		public ActorController(IActorService actorService)
		{
			_actorService = actorService;
		}

		// GET: /Actor/Dashboard/{id}
		[Route("Actor/Dashboard/{id:guid}")]
		public async Task<IActionResult> Index(Guid id)
		{
			var actor = await _actorService.GetByIdAsync(id);
			if (actor == null)
			{
				return NotFound();
			}

			return View(actor);
		}
	}
}