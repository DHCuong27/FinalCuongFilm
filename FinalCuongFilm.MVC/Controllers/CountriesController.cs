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
			string sortBy = "Latest",
			int pageNumber = 1,
			int pageSize = 12)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			var allCountries = await _countryService.GetAllAsync();
			var country = allCountries.FirstOrDefault(c => c.Slug == slug);

			// Navigation for genres and countries
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = await _genreService.GetAllAsync();
			ViewBag.Countries = await _countryService.GetAllAsync();

			if (country == null)
				return NotFound();

			var allMovies = await _movieService.GetAllAsync();

			var query = allMovies
					.Where(m => m.IsActive && m.SelectedCountryIds.Contains(country.Id))
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
				Genres = genres,
				Countries = allCountries,
				CountryId = country.Id,
				SortBy = sortBy,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalItems = totalItems,
				PageTitle = $"Country: {country.Name}",
				PageSubTitle = $"{totalItems} Film"
			};

			ViewBag.Country = country.Slug;
			return View(vm);
		}


	}

}
