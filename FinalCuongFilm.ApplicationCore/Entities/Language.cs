using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Language
	{ 
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;

		public ICollection<Movie> Movies { get; set; } = new List<Movie>();
	}
}
