namespace FinalCuongFilm.Common.DTOs
{
	public class FavoriteDto
	{
		public Guid Id { get; set; }
		public string UserId { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public Guid MovieId { get; set; }
		public string MovieTitle { get; set; } = string.Empty;
		public string? MoviePosterUrl { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class FavoriteCreateDto
	{
		public Guid MovieId { get; set; }
	}
}