//using FinalCuongFilm.ApplicationCore.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Favorite
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		

		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]


		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
