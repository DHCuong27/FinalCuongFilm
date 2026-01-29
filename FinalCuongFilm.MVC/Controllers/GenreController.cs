using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;

namespace FinalCuongFilm.MVC.Controllers
{
	public class GenreController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;

		public GenreController(IMovieService movieService, IGenreService genreService)
		{
			_movieService = movieService;
			_genreService = genreService;
		}

		// GET: /Genre/{slug}
		public async Task<IActionResult> Index(string slug)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			// Tìm genre theo slug
			var allGenres = await _genreService.GetAllAsync();
			var genre = allGenres.FirstOrDefault(g => g.Slug == slug);

			if (genre == null)
				return NotFound();

			// Lấy tất cả phim và filter theo genre
			var allMovies = await _movieService.GetAllAsync();
			var moviesInGenre = allMovies
				.Where(m => m.IsActive && m.SelectedGenreIds.Contains(genre.Id))
				.OrderByDescending(m => m.ReleaseYear)
				.ToList();

			ViewBag.GenreName = genre.Name;
			ViewBag.GenreSlug = genre.Slug;

			return View(moviesInGenre);
		}
	}
}