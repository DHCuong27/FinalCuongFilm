namespace FinalCuongFilm.Common.DTOs
{
	public class ActorDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? AvartUrl { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string? Gender { get; set; }
		public List<Guid> SelectedMovieIds { get; set; } = new List<Guid>();

		public List<string> ParticipatedMovieTitles { get; set; } = new List<string>();
		public List<ActorMovieDto> ParticipatedMovies { get; set; } = new List<ActorMovieDto>();
	}

	public class ActorMovieDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? PosterUrl { get; set; }
	}


	public class ActorCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public string? AvartUrl { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string? Gender { get; set; }
		public List<Guid> MovieIds { get; set; } = new();
	}

	public class ActorUpdateDto : ActorCreateDto
	{
		public Guid Id { get; set; }
	}
}