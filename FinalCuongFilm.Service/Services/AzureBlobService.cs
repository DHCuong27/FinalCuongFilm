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

		private const string VIDEO_CONTAINER = "videos";
		private const string POSTER_CONTAINER = "posters";
		private const string SUBTITLE_CONTAINER = "subtitles";

		private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mkv", ".avi", ".webm" };
		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
		private static readonly string[] AllowedSubtitleExtensions = { ".srt", ".vtt" };

		public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
		{
			var connectionString = configuration.GetConnectionString("AzureBlobStorage")
				?? throw new ArgumentNullException("AzureBlobStorage connection string not found");

			_blobServiceClient = new BlobServiceClient(connectionString);
			_logger = logger;

			_ = EnsureContainersExistAsync();
		}

		private async Task EnsureContainersExistAsync()
		{
			try
			{
				// ✅ FIX: Create PRIVATE containers (no public access)
				await CreateContainerIfNotExistsAsync(VIDEO_CONTAINER, PublicAccessType.None);
				await CreateContainerIfNotExistsAsync(POSTER_CONTAINER, PublicAccessType.None);
				await CreateContainerIfNotExistsAsync(SUBTITLE_CONTAINER, PublicAccessType.None);

				_logger.LogInformation("✅ All containers created with PRIVATE access. SAS tokens will be used for access.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error creating containers");
			}
		}

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

		public async Task<string> UploadPosterAsync(IFormFile file, string movieSlug)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(extension))
				throw new ArgumentException($"Invalid image format: {extension}");

			var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var fileName = $"{movieSlug}/poster-{timestamp}{extension}";

			return await UploadAsync(file, POSTER_CONTAINER, fileName);
		}

		public async Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedSubtitleExtensions.Contains(extension))
				throw new ArgumentException($"Invalid subtitle format: {extension}");

			var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var fileName = $"{movieSlug}/subtitles/{language}-{timestamp}{extension}";

			return await UploadAsync(file, SUBTITLE_CONTAINER, fileName);
		}

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
				_logger.LogInformation($"✅ Generated SAS URL for {blobUriBuilder.BlobName} (expires in {expiryHours}h)");

				return sasUri.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error generating SAS URL for: {blobUrl}");
				return blobUrl;
			}
		}

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

		public Task DeleteFileAsync(string fileUrl)
		{
			return DeleteAsync(fileUrl);
		}

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
				_ => "application/octet-stream"
			};
		}
	}
}