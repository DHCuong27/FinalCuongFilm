using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	[Table("Reviews")]
	public class Review
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		// Foreign Keys
		[Required]
		public string UserId { get; set; } = string.Empty; // AspNetUsers.Id

		[Required]
		public Guid MovieId { get; set; }

		// Review Content
		[Range(1, 5, ErrorMessage = "Rating must be 1 to 5")]
		public int Rating { get; set; } // 1-5 sao

		[MaxLength(1000)]
		public string? Comment { get; set; }

		// Status
		public bool IsApproved { get; set; } = false; // Admin duyệt

		// Timestamps
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }

		[ForeignKey("MovieId")]
		public Movie Movie { get; set; } = null!;
	}
}