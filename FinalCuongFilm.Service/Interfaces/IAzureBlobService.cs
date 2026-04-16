using System.IO;
using Microsoft.AspNetCore.Http;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IAzureBlobService
	{
		Task<string> UploadAsync(IFormFile file, string containerName, string? customFileName = null);
		Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null);
		Task<string> UploadPosterAsync(IFormFile file, string movieSlug);
		Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language);
		Task<bool> DeleteAsync(string blobUrl);
		Task DeleteFileAsync(string fileUrl);
		Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24);
		Task<bool> ExistsAsync(string blobUrl);
		Task<BlobMetadata> GetMetadataAsync(string blobUrl);
		Task<string> UploadStreamAsync(Stream stream, string fileName, string folderPath);
		//string GetSecureDownloadLink(string fileUrl, string containerName = "videos");
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