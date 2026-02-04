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
		private readonly string _connectionString;

		// Container names
		private const string VIDEO_CONTAINER = "videos";
		private const string POSTER_CONTAINER = "posters";
		private const string SUBTITLE_CONTAINER = "subtitles";

		// Allowed extensions
		private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mkv", ".avi", ".webm", ".m3u8" };
		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
		private static readonly string[] AllowedSubtitleExtensions = { ".srt", ".vtt" };

		public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
		{
			_connectionString = configuration.GetConnectionString("AzureBlobStorage")
				?? throw new ArgumentNullException("AzureBlobStorage connection string not found");

			_blobServiceClient = new BlobServiceClient(_connectionString);
			_logger = logger;

			// Ensure containers exist
			EnsureContainersExistAsync().Wait();
		}

		private async Task EnsureContainersExistAsync()
		{
			try
			{
				await CreateContainerIfNotExistsAsync(VIDEO_CONTAINER, PublicAccessType.None);
				await CreateContainerIfNotExistsAsync(POSTER_CONTAINER, PublicAccessType.Blob);
				await CreateContainerIfNotExistsAsync(SUBTITLE_CONTAINER, PublicAccessType.Blob);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating containers");
			}
		}

		private async Task CreateContainerIfNotExistsAsync(string containerName, PublicAccessType accessType)
		{
			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
			await containerClient.CreateIfNotExistsAsync(accessType);
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

		public async Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null, IProgress<int>? progress = null)
		{
			// Validate file
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedVideoExtensions.Contains(extension))
				throw new ArgumentException($"Invalid video format. Allowed: {string.Join(", ", AllowedVideoExtensions)}");

			if (file.Length > 5_000_000_000) // 5GB limit
				throw new ArgumentException("File size exceeds 5GB limit");

			// Generate file name
			var fileName = episodeNumber.HasValue
				? $"{movieSlug}/ep-{episodeNumber:D3}{extension}"
				: $"{movieSlug}/movie{extension}";

			var containerClient = _blobServiceClient.GetBlobContainerClient(VIDEO_CONTAINER);
			var blobClient = containerClient.GetBlobClient(fileName);

			// Set metadata
			var metadata = new Dictionary<string, string>
			{
				{ "MovieSlug", movieSlug },
				{ "OriginalFileName", file.FileName },
				{ "UploadedAt", DateTime.UtcNow.ToString("o") }
			};

			if (episodeNumber.HasValue)
				metadata["EpisodeNumber"] = episodeNumber.Value.ToString();

			var blobHttpHeaders = new BlobHttpHeaders
			{
				ContentType = "video/mp4",
				CacheControl = "public, max-age=31536000" // 1 year cache
			};

			try
			{
				using var stream = file.OpenReadStream();

				// Upload with progress tracking
				var uploadOptions = new BlobUploadOptions
				{
					HttpHeaders = blobHttpHeaders,
					Metadata = metadata,
					TransferOptions = new Azure.Storage.StorageTransferOptions
					{
						MaximumConcurrency = 4,
						InitialTransferSize = 8 * 1024 * 1024, // 8MB
						MaximumTransferSize = 4 * 1024 * 1024  // 4MB chunks
					}
				};

				if (progress != null)
				{
					var totalBytes = file.Length;
					var uploadedBytes = 0L;

					uploadOptions.ProgressHandler = new Progress<long>(bytes =>
					{
						uploadedBytes = bytes;
						var percentage = (int)((uploadedBytes * 100) / totalBytes);
						progress.Report(percentage);
					});
				}

				await blobClient.UploadAsync(stream, uploadOptions);

				_logger.LogInformation("Uploaded video {FileName} successfully", fileName);

				return blobClient.Uri.ToString();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error uploading video {FileName}", fileName);
				throw;
			}
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
				var blobClient = new BlobClient(new Uri(blobUrl), null);

				var containerName = blobClient.BlobContainerName;
				var blobName = blobClient.Name;

				var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
				var blob = containerClient.GetBlobClient(blobName);

				var response = await blob.DeleteIfExistsAsync();

				_logger.LogInformation("Deleted blob {BlobName} from {Container}", blobName, containerName);

				return response.Value;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting blob {BlobUrl}", blobUrl);
				return false;
			}
		}

		public async Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24)
		{
			try
			{
				var blobClient = new BlobClient(new Uri(blobUrl), null);

				// Recreate with connection string to get credentials
				var containerClient = _blobServiceClient.GetBlobContainerClient(blobClient.BlobContainerName);
				var blob = containerClient.GetBlobClient(blobClient.Name);

				// Check if blob exists
				if (!await blob.ExistsAsync())
					throw new FileNotFoundException($"Blob not found: {blobUrl}");

				// Generate SAS token
				var sasBuilder = new BlobSasBuilder
				{
					BlobContainerName = blob.BlobContainerName,
					BlobName = blob.Name,
					Resource = "b", // Blob
					StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
					ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
				};

				sasBuilder.SetPermissions(BlobSasPermissions.Read);

				var sasToken = blob.GenerateSasUri(sasBuilder);

				return sasToken.ToString();
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
				var blobClient = new BlobClient(new Uri(blobUrl), null);
				var containerClient = _blobServiceClient.GetBlobContainerClient(blobClient.BlobContainerName);
				var blob = containerClient.GetBlobClient(blobClient.Name);

				return await blob.ExistsAsync();
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
				var blobClient = new BlobClient(new Uri(blobUrl), null);
				var containerClient = _blobServiceClient.GetBlobContainerClient(blobClient.BlobContainerName);
				var blob = containerClient.GetBlobClient(blobClient.Name);

				var properties = await blob.GetPropertiesAsync();

				return new BlobMetadata
				{
					FileName = blob.Name,
					FileSize = properties.Value.ContentLength,
					ContentType = properties.Value.ContentType,
					LastModified = properties.Value.LastModified.DateTime,
					Url = blob.Uri.ToString()
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