using FinalCuongFilm.MVC.Models.ViewModels;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	public class CountriesController : Controller
	{
		private readonly ICountryService _countryService;
		private readonly IGenreService _genreService;
		private readonly IMovieService _movieService;

		public CountriesController(
			ICountryService countryService,
			IGenreService genreService,
			IMovieService movieService)
		{
			_countryService = countryService;
			_genreService = genreService;
			_movieService = movieService;
		}

		public async Task<IActionResult> Index(
		string slug,
		string sortBy = "latest", // ĐÃ FIX: Chữ 'l' viết thường cho đồng bộ với View
		int pageNumber = 1,
		int pageSize = 12)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			var allCountries = await _countryService.GetAllAsync();
			var country = allCountries.FirstOrDefault(c => c.Slug == slug);

			if (country == null)
				return NotFound();

			var allGenres = await _genreService.GetAllAsync();

			ViewBag.Genres = allGenres;
			ViewBag.Countries = allCountries;
			ViewBag.Country = country.Slug; // Giữ lại Slug để ném ra View

			// Retrieving and filtering movies
			var allMovies = await _movieService.GetAllAsync();

			var query = allMovies
					.Where(m => m.IsActive && m.CountryId == country.Id)
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

			// navigation
			var pagedMovies = filteredList
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToList();

			// Pack in VM
			var vm = new MovieFilterViewModel
			{
				Movies = pagedMovies,
				Genres = allGenres,
				Countries = allCountries,
				CountryId = country.Id,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = $"Country: {country.Name}",
				PageSubTitle = $"{totalItems} Film"
			};

			return View(vm);
		}
	}
}