// Kiểm tra entity có đầy đủ không
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class MediaFile
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string FileName { get; set; } = string.Empty;
		public string FileUrl { get; set; } = string.Empty;
		public string? FilePath { get; set; }
		public long? FileSizeBytes { get; set; }
		public string FileType { get; set; } = string.Empty; // video, subtitle
		public string? Quality { get; set; } // 1080p, 720p, 480p, 360p
		public string? Language { get; set; } // cho subtitle
		public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

		// Foreign Keys
		public Guid? MovieId { get; set; }
		[ForeignKey("MovieId")]
		public Movie? Movie { get; set; }

		public Guid? EpisodeId { get; set; }
		[ForeignKey("EpisodeId")]
		public Episode? Episode { get; set; }
	}
}