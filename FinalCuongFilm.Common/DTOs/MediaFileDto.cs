using Microsoft.AspNetCore.Http;

namespace FinalCuongFilm.Common.DTOs
{
	public class MediaFileDto
	{
		public Guid Id { get; set; }
		public string FileName { get; set; } = string.Empty;
		public string FileUrl { get; set; } = string.Empty;
		public string? FilePath { get; set; }
		public long? FileSizeBytes { get; set; }
		public string FileSizeFormatted => FormatFileSize(FileSizeBytes);
		public string FileType { get; set; } = string.Empty;
		public string? Quality { get; set; }
		public string? Language { get; set; }
		public DateTime UploadedAt { get; set; }

		public Guid? MovieId { get; set; }
		public string? MovieTitle { get; set; }

		public Guid? EpisodeId { get; set; }
		public string? EpisodeTitle { get; set; }
		public int? EpisodeNumber { get; set; }

		private static string FormatFileSize(long? bytes)
		{
			if (bytes == null) return "N/A";

			string[] sizes = { "B", "KB", "MB", "GB", "TB" };
			double len = bytes.Value;
			int order = 0;

			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len = len / 1024;
			}

			return $"{len:0.##} {sizes[order]}";
		}
	}

	public class MediaFileCreateDto
	{
		public string FileName { get; set; } = string.Empty;
		public string FileUrl { get; set; } = string.Empty;
		public string? FilePath { get; set; }
		public long? FileSizeBytes { get; set; }
		public string FileType { get; set; } = "Video";
		public string? Quality { get; set; }
		public string? Language { get; set; }

		public Guid? MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
	}

	public class MediaFileUpdateDto : MediaFileCreateDto
	{
		public Guid Id { get; set; }
	}

	// DTO cho upload file
	public class MediaFileUploadDto
	{
		public IFormFile File { get; set; } = null!;
		public string FileType { get; set; } = "Video";
		public string? Quality { get; set; }
		public string? Language { get; set; }

		public Guid? MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
	}

	public class VideoUploadDto
	{
		public Guid MovieId { get; set; }
		public Guid? EpisodeId { get; set; }
		public int? EpisodeNumber { get; set; }
		public IFormFile? VideoFile { get; set; }
		public string? ManualUrl { get; set; }
		public string Quality { get; set; } = "1080p";
		public string? Language { get; set; }
	}
}