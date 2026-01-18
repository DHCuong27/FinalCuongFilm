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
	public class WatchHistory
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Guid? EpisodeId { get; set; }
		[ForeignKey("EpisodeId")]

		public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

		
	}
}
