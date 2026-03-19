using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	[Table("Favorites")]
	public class Favorite
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public string UserId { get; set; } = string.Empty;

		[Required]
		public Guid MovieId { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		[ForeignKey("MovieId")]
		public Movie Movie { get; set; } = null!;
	}
}