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
	}

	public class ActorCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public string? AvartUrl { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string? Gender { get; set; }
	}

	public class ActorUpdateDto : ActorCreateDto
	{
		public Guid Id { get; set; }
	}
}