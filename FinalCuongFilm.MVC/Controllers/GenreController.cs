using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.MVC.Models.ViewModels;

namespace FinalCuongFilm.MVC.Controllers
{
	public class GenreController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;

		public GenreController(
			IMovieService movieService,
			IGenreService genreService,
			ICountryService countryService)
		{
			_movieService = movieService;
			_genreService = genreService;
			_countryService = countryService;
		}

		// GET: /Genre/{slug}?sortBy=&pageNumber=&pageSize=
		public async Task<IActionResult> Index(
			string slug,
			string sortBy = "latest",
			int pageNumber = 1,
			int pageSize = 12)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			var allGenres = await _genreService.GetAllAsync();
			var genre = allGenres.FirstOrDefault(g => g.Slug == slug);

			// Lấy genres và countries cho navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = await _genreService.GetAllAsync();
			ViewBag.Countries = await _countryService.GetAllAsync();

			if (genre == null)
				return NotFound();

			var allMovies = await _movieService.GetAllAsync();
		

			var query = allMovies
				.Where(m => m.IsActive && m.SelectedCountryIds.Contains(genre.Id))
				.AsEnumerable();

			query = sortBy switch
			{
				"popular" => query.OrderByDescending(m => m.ViewCount),
				"year_asc" => query.OrderBy(m => m.ReleaseYear),
				"title" => query.OrderBy(m => m.Title),
				_ => query.OrderByDescending(m => m.ReleaseYear)
			};

			var filteredList = query.ToList();
			var totalItems = filteredList.Count;
			var pagedMovies = filteredList
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			var vm = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Genres = allGenres,
				Countries = countries,
				GenreId = genre.Id,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = $"Genre: {genre.Name}",
				PageSubTitle = $"{totalItems} Film"
			};

			ViewBag.GenreSlug = genre.Slug;
			return View(vm);
		}
	}
}