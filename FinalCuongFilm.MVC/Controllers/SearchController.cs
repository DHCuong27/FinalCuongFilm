using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.MVC.Controllers
{
	public class SearchController : Controller
	{
		private readonly IMovieService _movieService;

		public SearchController(IMovieService movieService)
		{
			_movieService = movieService;
		}

		// GET: /Search?q={keyword}
		public async Task<IActionResult> Index(string q)
		{
			if (string.IsNullOrWhiteSpace(q))
			{
				ViewBag.Keyword = "";
				return View(new List<FinalCuongFilm.Common.DTOs.MovieDto>());
			}

			var allMovies = await _movieService.GetAllAsync();
			var searchResults = allMovies
				.Where(m => m.IsActive &&
					(m.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
					 (m.Description != null && m.Description.Contains(q, StringComparison.OrdinalIgnoreCase))))
				.OrderByDescending(m => m.ReleaseYear)
				.ToList();

			ViewBag.Keyword = q;
			return View(searchResults);
		}
	}
}