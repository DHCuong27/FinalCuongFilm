using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	[Table("Favorites")]
	public class Favorite
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		// Foreign Keys
		[Required]
		public string UserId { get; set; } = string.Empty; 

		[Required]
		public Guid MovieId { get; set; }

		// Timestamps
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation Properties
		[ForeignKey("UserId")]
		public IdentityUser User { get; set; } = null!;

		[ForeignKey("MovieId")]
		public Movie Movie { get; set; } = null!;
	}
}