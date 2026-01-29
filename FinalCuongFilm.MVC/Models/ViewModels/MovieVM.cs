using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.MVC.Models.ViewModels
{
	public class MovieDetailsViewModel
	{
		public MovieDto Movie { get; set; } = null!;
		public List<EpisodeDto> Episodes { get; set; } = new();
		public List<MediaFileDto> MediaFiles { get; set; } = new();
		public List<MovieDto> RelatedMovies { get; set; } = new();
	}

	public class MovieWatchViewModel
	{
		public MovieDto Movie { get; set; } = null!;
		public List<EpisodeDto> Episodes { get; set; } = new();
		public EpisodeDto? CurrentEpisode { get; set; }
		public List<MediaFileDto> MediaFiles { get; set; } = new();
	}
}