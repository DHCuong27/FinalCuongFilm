using Microsoft.AspNetCore.Http;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IAzureBlobService
	{
		/// <summary>
		/// Upload file lên Azure Blob Storage
		/// </summary>
		Task<string> UploadAsync(IFormFile file, string containerName, string? customFileName = null);

		/// <summary>
		/// Upload video với progress tracking
		/// </summary>
		Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null, IProgress<int>? progress = null);

		/// <summary>
		/// Upload poster/thumbnail
		/// </summary>
		Task<string> UploadPosterAsync(IFormFile file, string movieSlug);

		/// <summary>
		/// Upload subtitle file
		/// </summary>
		Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language);

		/// <summary>
		/// Xóa file từ Azure Blob
		/// </summary>
		Task<bool> DeleteAsync(string blobUrl);

		/// <summary>
		/// Lấy SAS token cho video streaming
		/// </summary>
		Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24);

		/// <summary>
		/// Kiểm tra file có tồn tại không
		/// </summary>
		Task<bool> ExistsAsync(string blobUrl);

		/// <summary>
		/// Lấy thông tin metadata của file
		/// </summary>
		Task<BlobMetadata> GetMetadataAsync(string blobUrl);
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