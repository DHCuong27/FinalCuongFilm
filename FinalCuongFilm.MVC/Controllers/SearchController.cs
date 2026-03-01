using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	public class SearchController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;

		// Đã sửa: Thêm IGenreService và ICountryService vào tham số và gán giá trị
		public SearchController(
			IMovieService movieService,
			IGenreService genreService,
			ICountryService countryService)
		{
			_movieService = movieService;
			_genreService = genreService;
			_countryService = countryService;
		}

		// GET: /Search?q={keyword}
		public async Task<IActionResult> Index(string q)
		{
			if (string.IsNullOrWhiteSpace(q))
			{
				ViewBag.Keyword = "";
				return View(new List<FinalCuongFilm.Common.DTOs.MovieDto>());
			}
			// Lấy genres và countries cho navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			var query = q.Trim().ToLower();
			var allMovies = await _movieService.GetAllAsync();

			// Lọc đa điều kiện: Tên phim, Mô tả, Quốc gia, Thể loại, Diễn viên
			var searchResults = allMovies
				.Where(m => m.IsActive && (
					(m.Title != null && m.Title.ToLower().Contains(query)) ||
					(m.Description != null && m.Description.ToLower().Contains(query)) ||
					(m.CountryName != null && m.CountryName.ToLower().Contains(query))
				// Bỏ comment 2 dòng dưới nếu MovieDto của bạn chứa list Actors và Genres
				//= || (m.Genres != null && m.Genres.Any(g => g.Name.ToLower().Contains(query)))
				// || (m.Actors != null && m.Actors.Any(a => a.Name.ToLower().Contains(query)))
				))
				.OrderByDescending(m => m.ReleaseYear)
				.ToList();

			ViewBag.Keyword = q;
			return View(searchResults);
		}

		// API GET: /Search/Suggestions?q={keyword}
		[HttpGet]
		public async Task<IActionResult> Suggestions(string q)
		{
			if (string.IsNullOrWhiteSpace(q))
				return Json(new List<object>());

			var query = q.Trim().ToLower();
			var allMovies = await _movieService.GetAllAsync();

			// Trả về tối đa 5 kết quả rút gọn cho Dropdown
			var suggestions = allMovies
				.Where(m => m.IsActive && m.Title != null && m.Title.ToLower().Contains(query))
				.OrderByDescending(m => m.ReleaseYear)
				.Take(5)
				.Select(m => new
				{
					id = m.Id,
					title = m.Title,
					slug = m.Slug,
					posterUrl = m.PosterUrl ?? "/images/no-poster.jpg",
					year = m.ReleaseYear
				})
				.ToList();

			return Json(suggestions);
		}
	}
}