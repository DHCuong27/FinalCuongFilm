using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Movie_Genre
	{
		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; }

		public Guid GenreId { get; set; }
		[ForeignKey("GenreId")]
		public Genre? Genre { get; set; }
	}


}
