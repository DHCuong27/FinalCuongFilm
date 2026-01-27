//namespace FinalCuongFilm.Common.DTOs
//{
//	public class EpisodeDto
//	{
//		public Guid Id { get; set; }
//		public int EpisodeNumber { get; set; }
//		public string Title { get; set; } = string.Empty;
//		public string? Description { get; set; }
//		public int? DurationMinutes { get; set; }
//		public DateTime? AirDate { get; set; }
//		public long ViewCount { get; set; }
//		public bool IsActive { get; set; }

//		public Guid MovieId { get; set; }
//		public string? MovieTitle { get; set; }
//	}

//	public class EpisodeCreateDto
//	{
//		public int EpisodeNumber { get; set; }
//		public string Title { get; set; } = string.Empty;
//		public string? Description { get; set; }
//		public int? DurationMinutes { get; set; }
//		public DateTime? AirDate { get; set; }
//		public bool IsActive { get; set; } = true;

//		public Guid MovieId { get; set; }
//	}

//	public class EpisodeUpdateDto : EpisodeCreateDto
//	{
//		public Guid Id { get; set; }
//	}
//}