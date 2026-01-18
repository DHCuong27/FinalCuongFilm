using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Episode
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid MovieId { get; set; }
		[ForeignKey("MovieId")]
		public string Title { get; set; } = string.Empty;


		public int EpisodeNumber { get; set; }
	
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
		public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
	}
}
