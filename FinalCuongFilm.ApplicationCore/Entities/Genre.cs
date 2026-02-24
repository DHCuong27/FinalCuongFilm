using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Genre
	{

		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; } = string.Empty;
		public string Slug { get; set; }	= string.Empty;

		public string? Description { get; set; }

		public ICollection<MovieGenre> Movie_Genres { get; set; } = new List<MovieGenre>();
	}
}
