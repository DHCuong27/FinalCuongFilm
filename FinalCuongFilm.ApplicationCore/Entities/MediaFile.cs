using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class MediaFile
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string FileName { get; set; } = string.Empty;

		public string Quality { get; set; } = string.Empty;

		public string FileFormat { get; set; } = string.Empty;
		public long FileSizeInBytes { get; set; }

		public Guid MovieId { get; set; }
		public Movie? Movie { get; set; } 

		public Guid? EpisodeId { get; set; }
		public Episode? Episode { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
