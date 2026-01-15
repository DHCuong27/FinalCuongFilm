using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Movie_Tag
	{
		public Guid MovieId { get; set; }
		public Movie? Movie { get; set; } 

		public Guid TagId { get; set; }
		public Tag? Tag { get; set; } 
	}

}
