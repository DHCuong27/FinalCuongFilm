using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class MovieActor
	{

		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; }

		public Guid ActorId { get; set; }
		[ForeignKey("ActorId")]
		public Actor? Actor { get; set; }
	}

}
