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

			// ✅ FIX: Gọi async method đúng cách
			_ = EnsureContainersExistAsync();
		}

		private async Task EnsureContainersExistAsync()
		{
			try
			{
				// ✅ FIX: Tạo container PRIVATE (không public)
				await CreateContainerIfNotExistsAsync(VIDEO_CONTAINER);
				await CreateContainerIfNotExistsAsync(POSTER_CONTAINER);
				await CreateContainerIfNotExistsAsync(SUBTITLE_CONTAINER);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating containers");
			}
		}

		// ✅ FIX: Bỏ PublicAccessType parameter
		private async Task CreateContainerIfNotExistsAsync(string containerName)
		{
			try
			{
				var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

				// Tạo container với access level = Private (default)
				var response = await containerClient.CreateIfNotExistsAsync();

				if (response != null && response.Value != null)
				{
					_logger.LogInformation("Created container: {ContainerName}", containerName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating container {ContainerName}", containerName);
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

			var blobHttpHeaders = new BlobHttpHeaders
			{
				ContentType = file.ContentType
			};

			using var stream = file.OpenReadStream();
			await blobClient.UploadAsync(stream, new BlobUploadOptions
			{
				HttpHeaders = blobHttpHeaders
			});

			_logger.LogInformation("Uploaded {FileName} to {Container}", fileName, containerName);

			return blobClient.Uri.ToString();
		}

		public async Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedVideoExtensions.Contains(extension))
				throw new ArgumentException($"Invalid video format. Allowed: {string.Join(", ", AllowedVideoExtensions)}");

			if (file.Length > 5_000_000_000) // 5GB limit
				throw new ArgumentException("File size exceeds 5GB limit");

			var fileName = episodeNumber.HasValue
				? $"{movieSlug}/ep-{episodeNumber:D3}{extension}"
				: $"{movieSlug}/movie{extension}";

			return await UploadAsync(file, VIDEO_CONTAINER, fileName);
		}

		public async Task<string> UploadPosterAsync(IFormFile file, string movieSlug)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(extension))
				throw new ArgumentException($"Invalid image format. Allowed: {string.Join(", ", AllowedImageExtensions)}");

			if (file.Length > 10_000_000) // 10MB limit
				throw new ArgumentException("Image size exceeds 10MB limit");

			var fileName = $"{movieSlug}/poster{extension}";
			return await UploadAsync(file, POSTER_CONTAINER, fileName);
		}

		public async Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedSubtitleExtensions.Contains(extension))
				throw new ArgumentException($"Invalid subtitle format. Allowed: {string.Join(", ", AllowedSubtitleExtensions)}");

			var fileName = $"{movieSlug}/subtitles/{language}{extension}";
			return await UploadAsync(file, SUBTITLE_CONTAINER, fileName);
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

				_logger.LogInformation("Deleted blob {BlobName} from {Container}",
					blobUriBuilder.BlobName, blobUriBuilder.BlobContainerName);

				return response.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting blob {BlobUrl}", blobUrl);
				return false;
			}
		}

		public async Task DeleteFileAsync(string fileUrl)
		{
			try
			{
				var uri = new Uri(fileUrl);
				var blobUriBuilder = new BlobUriBuilder(uri);

				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

				await blobClient.DeleteIfExistsAsync();
				_logger.LogInformation("Deleted file from Azure: {FileUrl}", fileUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting file from Azure: {FileUrl}", fileUrl);
				throw;
			}
		}

		public async Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24)
		{
			try
			{
				var uri = new Uri(blobUrl);
				var blobUriBuilder = new BlobUriBuilder(uri);

				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

				// Check if blob exists
				if (!await blobClient.ExistsAsync())
					throw new FileNotFoundException($"Blob not found: {blobUrl}");

				// ✅ Generate SAS token cho private blob
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

				return sasUri.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating streaming URL for {BlobUrl}", blobUrl);
				throw;
			}
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
			try
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
					Url = blobClient.Uri.ToString()
				};
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting metadata for {BlobUrl}", blobUrl);
				throw;
			}
		}
	}
}