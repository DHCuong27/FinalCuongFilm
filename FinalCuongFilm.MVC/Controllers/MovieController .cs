using Microsoft.AspNetCore.Mvc;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.MVC.Models.ViewModels;

namespace FinalCuongFilm.MVC.Controllers
{
	public class MovieController : Controller
	{
		private readonly IMovieService _movieService;
		private readonly IEpisodeService _episodeService;
		private readonly IMediaFileService _mediaFileService;
		private readonly IGenreService _genreService;
		private readonly ICountryService _countryService;

		public MovieController(
			IMovieService movieService,
			IEpisodeService episodeService,
			IMediaFileService mediaFileService,
			IGenreService genreService,
			ICountryService countryService)
		{
			_movieService = movieService;
			_episodeService = episodeService;
			_mediaFileService = mediaFileService;
			_genreService = genreService;
			_countryService = countryService;
		}

		// GET: /Movie/Detail/{slug}
		public async Task<IActionResult> Detail(string slug)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			// Tìm phim theo slug
			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

			if (movie == null)
				return NotFound();

			// Lấy episodes nếu là phim bộ
			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);

			// Lấy media files
			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

			// Lấy phim liên quan (cùng thể loại hoặc cùng quốc gia)
			var relatedMovies = allMovies
				.Where(m => m.IsActive &&
							m.Id != movie.Id &&
							(m.CountryId == movie.CountryId ||
							 m.SelectedGenreIds.Intersect(movie.SelectedGenreIds).Any()))
				.OrderByDescending(m => m.ViewCount)
				.Take(6)
				.ToList();

			// ✅ Load Genres và Countries cho header navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			var viewModel = new MovieDetailsViewModel
			{
				Movie = movie,
				Episodes = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList(),
				MediaFiles = mediaFiles.ToList(),
				RelatedMovies = relatedMovies
			};

			return View(viewModel);
		}

		// GET: /Movie/Watch/{slug}?ep={episodeNumber}
		public async Task<IActionResult> Watch(string slug, int? ep)
		{
			if (string.IsNullOrEmpty(slug))
				return NotFound();

			var allMovies = await _movieService.GetAllAsync();
			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

			if (movie == null)
				return NotFound();

			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
			var episodesList = episodes.Where(e => e.IsActive).OrderBy(e => e.EpisodeNumber).ToList();

			// Xác định episode cần xem
			var currentEpisode = ep.HasValue
				? episodesList.FirstOrDefault(e => e.EpisodeNumber == ep.Value)
				: episodesList.FirstOrDefault();

			// Lấy media files
			var mediaFiles = currentEpisode != null
				? await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id)
				: await _mediaFileService.GetByMovieIdAsync(movie.Id);

			// ✅ Load Genres và Countries cho header navigation
			var genres = await _genreService.GetAllAsync();
			var countries = await _countryService.GetAllAsync();

			ViewBag.Genres = genres;
			ViewBag.Countries = countries;

			var viewModel = new MovieWatchViewModel
			{
				Movie = movie,
				Episodes = episodesList,
				CurrentEpisode = currentEpisode,
				MediaFiles = mediaFiles.Where(m => m.FileType == "Video").ToList()
			};

			return View(viewModel);
		}
	}
}