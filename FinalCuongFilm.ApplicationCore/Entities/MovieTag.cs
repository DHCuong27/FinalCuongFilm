using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class MovieTag
	{
		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; } 

		public Guid TagId { get; set; }
		[ForeignKey("TagId")]
		public Tag? Tag { get; set; } 
	}

}
