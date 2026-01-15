using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Movie_Genre
	{
		public Guid MovieId { get; set; }
		public Movie? Movie { get; set; }

		public Guid GenreId { get; set; }
		public Genre? Genre { get; set; }
	}


}
