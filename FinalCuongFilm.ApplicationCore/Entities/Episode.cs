using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Episode
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public Guid MovieId { get; set; }
		public string Title { get; set; } = string.Empty;


		public int EpisodeNumber { get; set; }
	
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	
		public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
	}
}
