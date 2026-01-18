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
	public class Review
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public int Score { get; set; }


		public string? Comment { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; } 
		public Guid  MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; } 
	}
}
