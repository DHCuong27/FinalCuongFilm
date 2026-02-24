using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
