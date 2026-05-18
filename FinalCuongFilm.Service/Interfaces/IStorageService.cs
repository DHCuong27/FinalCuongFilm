using System.IO;
using Microsoft.AspNetCore.Http;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IStorageService
	{
		Task<string> UploadAsync(IFormFile file, string bucketName, string? customFileName = null);
		Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null);
		Task<string> UploadPosterAsync(IFormFile file, string movieSlug);
		Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language);
		Task<bool> DeleteAsync(string fileUrl);
		Task DeleteFileAsync(string fileUrl);
		Task<string> GetStreamingUrlAsync(string fileUrl, int expiryHours = 24);
		Task<bool> ExistsAsync(string fileUrl);
		Task<BlobMetadata> GetMetadataAsync(string fileUrl);
		Task<string> UploadStreamAsync(Stream stream, string fileName, string folderPath);
	}

	public class BlobMetadata
	{
		public string FileName { get; set; } = string.Empty;
		public long FileSize { get; set; }
		public string ContentType { get; set; } = string.Empty;
		public DateTime? LastModified { get; set; }
		public string Url { get; set; } = string.Empty;
	}
}