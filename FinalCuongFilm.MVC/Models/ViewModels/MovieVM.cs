using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.MVC.Models.ViewModels
{
	public class MovieFilterViewModel
	{
	
		public List<MovieDto> Movies { get; set; } = new();

		public IEnumerable<GenreDto> Genres { get; set; } = new List<GenreDto>();
		public IEnumerable<CountryDto> Countries { get; set; } = new List<CountryDto>();

		public string? Search { get; set; }
		public Guid? GenreId { get; set; }
		public Guid? CountryId { get; set; }
		public int? ReleaseYear { get; set; }
		public int? Type { get; set; }       // MovieType enum as int
		public string SortBy { get; set; } = "latest";

		// Phân trang
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 12;
		public int TotalItems { get; set; }
		public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
		public bool HasPreviousPage => PageNumber > 1;
		public bool HasNextPage => PageNumber < TotalPages;

		// Tiêu đề trang (dùng cho Genre/Country page)
		public string? PageTitle { get; set; }
		public string? PageSubTitle { get; set; }
	}


	public class HomeFilterViewModel
	{

		public List<MovieDto> LatestMovies { get; set; } = new();
		public List<MovieDto> PopularMovies { get; set; } = new();
		public MovieFilterViewModel AllMoviesFilter { get; set; } = new();


		public List<MovieDto> ContinueWatchingMovies { get; set; } = new();


		public List<MovieDto> KoreanMovies { get; set; } = new();
		public List<MovieDto> ChineseMovies { get; set; } = new();
	}
	public class MovieDetailsViewModel
	{
		public MovieDto Movie { get; set; } = null!;
		public List<EpisodeDto> Episodes { get; set; } = new();
		public List<MediaFileDto> MediaFiles { get; set; } = new();
		public List<MovieDto> RelatedMovies { get; set; } = new();
		public bool IsVipOnly { get; set; }

		public IEnumerable<ActorDto> Actors { get; set; } 

	}
	public class ManageVideosViewModel
	{
		// 1. Movie Information (Display only)
		public Guid MovieId { get; set; } // Mapped to your Guid Id
		public string MovieTitle { get; set; }
		public string? PosterUrl { get; set; }
		public bool IsSeries { get; set; }

		// 2. Existing Data
		public List<MediaFile> ExistingVideos { get; set; } = new List<MediaFile>();

		// 3. Upload Form Fields
		public IFormFile? VideoFile { get; set; }

		// Only required if IsSeries is true
		public int? EpisodeNumber { get; set; }

		public string Quality { get; set; } = "1080p";
	}

	public class MovieWatchViewModel
	{
		public MovieDto Movie { get; set; } = null!;
		public List<EpisodeDto> Episodes { get; set; } = new();
		public EpisodeDto? CurrentEpisode { get; set; }
		public List<MediaFileDto> MediaFiles { get; set; } = new();

		public bool IsVipOnly { get; set; }
	}

	
}