using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinalCuongFilm.Service.Services
{
	public class AzureBlobService : IAzureBlobService
	{
		private readonly BlobServiceClient _blobServiceClient;
		private readonly ILogger<AzureBlobService> _logger;
		private readonly IConfiguration _configuration;

		private const string VIDEO_CONTAINER = "videos";
		private const string POSTER_CONTAINER = "posters";
		private const string SUBTITLE_CONTAINER = "subtitles";

		private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mkv", ".avi", ".webm" };
		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
		private static readonly string[] AllowedSubtitleExtensions = { ".srt", ".vtt" };

		public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
		{
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

			var connectionString = _configuration.GetConnectionString("AzureBlobStorage")
				?? throw new ArgumentNullException("AzureBlobStorage connection string not found");

			_blobServiceClient = new BlobServiceClient(connectionString);
			_logger = logger;

			_ = EnsureContainersExistAsync();
		}

		// Ensure the necessary containers exist with appropriate access levels
		private async Task EnsureContainersExistAsync()
		{
			try
			{
				//  Create PRIVATE containers (no public access)
				await CreateContainerIfNotExistsAsync(VIDEO_CONTAINER, PublicAccessType.None);
				await CreateContainerIfNotExistsAsync(POSTER_CONTAINER, PublicAccessType.None);
				await CreateContainerIfNotExistsAsync(SUBTITLE_CONTAINER, PublicAccessType.None);

				_logger.LogInformation(" All containers created with PRIVATE access. SAS tokens will be used for access.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error creating containers");
			}
		}

		// Create container if it doesn't exist, with specified access type
		private async Task CreateContainerIfNotExistsAsync(string containerName, PublicAccessType accessType = PublicAccessType.None)
		{
			try
			{
				var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

				var response = await containerClient.CreateIfNotExistsAsync(accessType);

				if (response != null && response.Value != null)
				{
					_logger.LogInformation($"Created container: {containerName} with access: {accessType}");
				}
				else
				{
					_logger.LogInformation($"Container already exists: {containerName}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error with container {containerName}");
				throw;
			}
		}

		// Upload file to specified container, with optional custom filename
		public async Task<string> UploadAsync(IFormFile file, string containerName, string? customFileName = null)
		{
			if (file == null || file.Length == 0)
				throw new ArgumentException("File is empty");

			var fileName = customFileName ?? Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			var blobClient = containerClient.GetBlobClient(fileName);

			var contentType = GetContentType(Path.GetExtension(file.FileName));
			var blobHttpHeaders = new BlobHttpHeaders
			{
				ContentType = contentType,
				CacheControl = "public, max-age=31536000"
			};

			using var stream = file.OpenReadStream();
			await blobClient.UploadAsync(stream, new BlobUploadOptions
			{
				HttpHeaders = blobHttpHeaders
			});

			_logger.LogInformation($"Uploaded {fileName} to {containerName} with ContentType: {contentType}");

			return blobClient.Uri.ToString();
		}

		// Specialized upload methods for different media types
		public async Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedVideoExtensions.Contains(extension))
				throw new ArgumentException($"Invalid video format: {extension}");

			var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var fileName = episodeNumber.HasValue
				? $"{movieSlug}/episodes/ep{episodeNumber:D3}-{timestamp}{extension}"
				: $"{movieSlug}/movie-{timestamp}{extension}";

			return await UploadAsync(file, VIDEO_CONTAINER, fileName);
		}


		// Upload poster with organized folder structure
		public async Task<string> UploadPosterAsync(IFormFile file, string movieSlug)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(extension))
				throw new ArgumentException($"Invalid image format: {extension}");

			var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var fileName = $"{movieSlug}/poster-{timestamp}{extension}";

			return await UploadAsync(file, POSTER_CONTAINER, fileName);
		}

		// Upload subtitle with organized folder structure
		public async Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedSubtitleExtensions.Contains(extension))
				throw new ArgumentException($"Invalid subtitle format: {extension}");

			var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var fileName = $"{movieSlug}/subtitles/{language}-{timestamp}{extension}";

			return await UploadAsync(file, SUBTITLE_CONTAINER, fileName);
		}

		// Generate a streaming URL with SAS token for secure access
		public async Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24)
		{
			try
			{
				var uri = new Uri(blobUrl);
				var blobUriBuilder = new BlobUriBuilder(uri);

				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

				if (!await blobClient.ExistsAsync())
				{
					_logger.LogWarning($"Blob not found: {blobUrl}");
					return blobUrl;
				}

				if (!blobClient.CanGenerateSasUri)
				{
					_logger.LogWarning("Cannot generate SAS token. Returning direct URL");
					return blobUrl;
				}

				var sasBuilder = new BlobSasBuilder
				{
					BlobContainerName = blobUriBuilder.BlobContainerName,
					BlobName = blobUriBuilder.BlobName,
					Resource = "b",
					StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
					ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
				};

				sasBuilder.SetPermissions(BlobSasPermissions.Read);

				var sasUri = blobClient.GenerateSasUri(sasBuilder);
				_logger.LogInformation($" Generated SAS URL for {blobUriBuilder.BlobName} (expires in {expiryHours}h)");

				return sasUri.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error generating SAS URL for: {blobUrl}");
				return blobUrl;
			}
		}

		// Delete blob by URL
		public async Task<bool> DeleteAsync(string blobUrl)
		{
			try
			{
				var uri = new Uri(blobUrl);
				var blobUriBuilder = new BlobUriBuilder(uri);

				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

				var response = await blobClient.DeleteIfExistsAsync();

				if (response.Value)
				{
					_logger.LogInformation($"Deleted blob: {blobUriBuilder.BlobName}");
				}

				return response.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting blob: {blobUrl}");
				return false;
			}
		}

		// Specialized delete methods for different media types (optional, can also use DeleteAsync directly)
		public Task DeleteFileAsync(string fileUrl)
		{
			return DeleteAsync(fileUrl);
		}

		// Check if blob exists by URL
		public async Task<bool> ExistsAsync(string blobUrl)
		{
			try
			{
				var uri = new Uri(blobUrl);
				var blobUriBuilder = new BlobUriBuilder(uri);

				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

				return await blobClient.ExistsAsync();
			}
			catch
			{
				return false;
			}
		}

		// Get blob metadata by URL
		public async Task<BlobMetadata> GetMetadataAsync(string blobUrl)
		{
			var uri = new Uri(blobUrl);
			var blobUriBuilder = new BlobUriBuilder(uri);

			var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
			var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

			var properties = await blobClient.GetPropertiesAsync();

			return new BlobMetadata
			{
				FileName = blobUriBuilder.BlobName,
				FileSize = properties.Value.ContentLength,
				ContentType = properties.Value.ContentType,
				LastModified = properties.Value.LastModified.DateTime,
				Url = blobUrl
			};
		}

		// Upload a stream with specified filename and folder path, returning the blob URL
		public async Task<string> UploadStreamAsync(Stream stream, string fileName, string folderPath)
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(VIDEO_CONTAINER);
			var blobName = string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath.TrimEnd('/')}/{fileName}";
			var blobClient = containerClient.GetBlobClient(blobName);

			var contentType = GetContentType(Path.GetExtension(fileName));
			var blobHttpHeaders = new BlobHttpHeaders
			{
				ContentType = contentType,
				CacheControl = "public, max-age=31536000"
			};

			await blobClient.UploadAsync(stream, new BlobUploadOptions
			{
				HttpHeaders = blobHttpHeaders
			});

			_logger.LogInformation($"Uploaded stream as {blobName} to {VIDEO_CONTAINER} with ContentType: {contentType}");

			return blobClient.Uri.ToString();
		}

		// Map file extensions to content types for proper handling in Azure Blob Storage
		private string GetContentType(string fileExtension)
		{
			return fileExtension.ToLowerInvariant() switch
			{
				".mp4" => "video/mp4",
				".webm" => "video/webm",
				".ogg" => "video/ogg",
				".mov" => "video/quicktime",
				".avi" => "video/x-msvideo",
				".mkv" => "video/x-matroska",
				".m4v" => "video/x-m4v",
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".webp" => "image/webp",
				".srt" => "text/plain",
				".vtt" => "text/vtt",
				".m3u8" => "application/x-mpegURL", //  HLS playlist
				".ts" => "video/MP2T",              //  HLS segment
				_ => "application/octet-stream"
			};
		}

		// Lưu ý: Đổi "videos" thành tên container chứa phim của bạn
		public string GetSecureDownloadLink(string fileUrl, string containerName = "videos")
		{
			try
			{
				// 1. Lấy tên file (blob name) từ đường link VideoUrl gốc
				Uri uri = new Uri(fileUrl);
				string blobName = uri.Segments.Last();

				// 2. Khởi tạo BlobClient
				var blobServiceClient = new BlobServiceClient(_configuration.GetConnectionString("AzureBlobStorage"));
				var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
				var blobClient = blobContainerClient.GetBlobClient(blobName);

				// Kiểm tra xem có quyền tạo SAS không
				if (blobClient.CanGenerateSasUri)
				{
					// 3. Cấu hình SAS Token
					BlobSasBuilder sasBuilder = new BlobSasBuilder()
					{
						BlobContainerName = blobContainerClient.Name,
						BlobName = blobClient.Name,
						Resource = "b", // "b" nghĩa là quyền áp dụng cho một Blob (file) cụ thể
						StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Tránh lỗi lệch múi giờ
						ExpiresOn = DateTimeOffset.UtcNow.AddHours(2)    // Link chỉ sống trong 2 tiếng
					};

					// 4. Cấp quyền Đọc (Read) để tải về
					sasBuilder.SetPermissions(BlobSasPermissions.Read);

					// 5. TRICK QUAN TRỌNG: Ép trình duyệt Tải file thay vì mở video
					// Giải mã URL (để xóa ký tự %20 nếu có) và gán vào tên file tải về
					string fileNameToSave = Uri.UnescapeDataString(blobName);
					sasBuilder.ContentDisposition = $"attachment; filename=\"{fileNameToSave}\"";

					// 6. Tạo URI cuối cùng
					Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
					return sasUri.ToString();
				}

				return fileUrl; // Nếu lỗi, trả về link gốc
			}
			catch (Exception ex)
			{
				// Xử lý log lỗi nếu cần
				Console.WriteLine($"Lỗi tạo SAS Token: {ex.Message}");
				return null;
			}
		}
	}
}