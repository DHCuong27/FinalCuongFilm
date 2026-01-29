using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Actor
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;

		public int Age { get; set; }

		public string Slug { get; set; } = string.Empty;

		public string? AvartUrl { get; set; }
		public DateTime? DateOfBirth { get; set; }
		public string? Gender { get; set; }

		//public ICollection<Movie> Movies { get; set; } = new List<Movie>();
		public ICollection<Movie_Actor> Movie_Actors { get; set; } = new List<Movie_Actor>();
	}
}
