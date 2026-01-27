namespace FinalCuongFilm.Common.DTOs
{
	public class GenreDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? Description { get; set; }
	}

	public class GenreCreateDto
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
	}

	public class GenreUpdateDto : GenreCreateDto
	{
		public Guid Id { get; set; }
	}
}