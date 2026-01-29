//using Microsoft.AspNetCore.Mvc;
//using FinalCuongFilm.Service.Interfaces;
//using FinalCuongFilm.MVC.Models.ViewModels;

//namespace FinalCuongFilm.MVC.Controllers
//{
//	public class MovieController : Controller
//	{
//		private readonly IMovieService _movieService;
//		//private readonly IEpisodeService _episodeService;
//		//private readonly IMediaFileService _mediaFileService;
//		private readonly IGenreService _genreService;

//		public MovieController(
//			IMovieService movieService,
//			//IEpisodeService episodeService,
//			//IMediaFileService mediaFileService,
//			IGenreService genreService)
//		{
//			_movieService = movieService;
//			_episodeService = episodeService;
//			_mediaFileService = mediaFileService;
//			_genreService = genreService;
//		}

//		// GET: /Movie/Details/{slug}
//		public async Task<IActionResult> Details(string slug)
//		{
//			if (string.IsNullOrEmpty(slug))
//				return NotFound();

//			// Tìm phim theo slug
//			var allMovies = await _movieService.GetAllAsync();
//			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

//			if (movie == null)
//				return NotFound();

//			// Lấy episodes nếu là phim bộ
//			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);

//			// Lấy media files
//			var mediaFiles = await _mediaFileService.GetByMovieIdAsync(movie.Id);

//			// Lấy phim liên quan (cùng thể loại)
//			var relatedMovies = allMovies
//				.Where(m => m.IsActive && m.Id != movie.Id)
//				.Take(6)
//				.ToList();

//			var viewModel = new MovieDetailsViewModel
//			{
//				Movie = movie,
//				Episodes = episodes.OrderBy(e => e.EpisodeNumber).ToList(),
//				MediaFiles = mediaFiles.ToList(),
//				RelatedMovies = relatedMovies
//			};

//			return View(viewModel);
//		}

//		// GET: /Movie/Watch/{slug}?ep={episodeNumber}
//		public async Task<IActionResult> Watch(string slug, int? ep)
//		{
//			if (string.IsNullOrEmpty(slug))
//				return NotFound();

//			var allMovies = await _movieService.GetAllAsync();
//			var movie = allMovies.FirstOrDefault(m => m.Slug == slug && m.IsActive);

//			if (movie == null)
//				return NotFound();

//			var episodes = await _episodeService.GetByMovieIdAsync(movie.Id);
//			var episodesList = episodes.OrderBy(e => e.EpisodeNumber).ToList();

//			// Xác định episode cần xem
//			var currentEpisode = ep.HasValue
//				? episodesList.FirstOrDefault(e => e.EpisodeNumber == ep.Value)
//				: episodesList.FirstOrDefault();

//			if (currentEpisode == null && episodesList.Any())
//			{
//				currentEpisode = episodesList.First();
//			}

//			// Lấy media files của episode hoặc movie
//			var mediaFiles = currentEpisode != null
//				? await _mediaFileService.GetByEpisodeIdAsync(currentEpisode.Id)
//				: await _mediaFileService.GetByMovieIdAsync(movie.Id);

//			var viewModel = new MovieWatchViewModel
//			{
//				Movie = movie,
//				Episodes = episodesList,
//				CurrentEpisode = currentEpisode,
//				MediaFiles = mediaFiles.Where(m => m.FileType == "Video").ToList()
//			};

//			return View(viewModel);
//		}
//	}
//}