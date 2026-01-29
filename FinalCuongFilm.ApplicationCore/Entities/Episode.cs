using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	[Table("Episodes")]
	public class Episode
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public int EpisodeNumber { get; set; }

		[Required]
		[MaxLength(255)]
		public string Title { get; set; } = string.Empty;

		[MaxLength(2000)]
		public string? Description { get; set; }

		public int? DurationMinutes { get; set; }

		public DateTime? AirDate { get; set; }

		public long ViewCount { get; set; } = 0;

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? UpdatedAt { get; set; }

		// Foreign Key
		[Required]
		public Guid MovieId { get; set; }

		// Navigation Properties
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; }

		public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
	}
}