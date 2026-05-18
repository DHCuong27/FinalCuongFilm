//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
//using Azure.Storage.Sas;
//using FinalCuongFilm.Service.Interfaces;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace FinalCuongFilm.Service.Services
//{
//	public class AzureBlobService : IStorageService
//	{
//		private readonly BlobServiceClient? _blobServiceClient;
//		private readonly ILogger<AzureBlobService> _logger;
//		private readonly IConfiguration _configuration;

//		private const string VIDEO_CONTAINER = "videos";
//		private const string POSTER_CONTAINER = "posters";
//		private const string SUBTITLE_CONTAINER = "subtitles";

//		private static readonly string[] AllowedVideoExtensions = { ".mp4", ".mkv", ".avi", ".webm", ".m3u8" };
//		private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
//		private static readonly string[] AllowedSubtitleExtensions = { ".srt", ".vtt" };

//		public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
//		{
//			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
//			_logger = logger ?? throw new ArgumentNullException(nameof(logger));

//			var connectionString = _configuration.GetConnectionString("AzureBlobStorage");

//			// KIỂM TRA THÔNG MINH: Nếu không có Connection String hoặc là "fake", Service vẫn sống nhưng báo Warning
//			if (!string.IsNullOrEmpty(connectionString) && connectionString != "fake")
//			{
//				try
//				{
//					_blobServiceClient = new BlobServiceClient(connectionString);
//					// Chạy khởi tạo container ngầm
//					_ = EnsureContainersExistAsync();
//				}
//				catch (Exception ex)
//				{
//					_logger.LogError(ex, "❌ Cấu hình Azure Connection String không hợp lệ.");
//				}
//			}
//			else
//			{
//				_logger.LogWarning("⚠️ CẢNH BÁO: Azure Storage chưa được cấu hình. Chức năng media sẽ bị lỗi.");
//			}
//		}

//		private async Task EnsureContainersExistAsync()
//		{
//			if (_blobServiceClient == null) return;
//			try
//			{
//				await CreateContainerIfNotExistsAsync(VIDEO_CONTAINER, PublicAccessType.None);
//				await CreateContainerIfNotExistsAsync(POSTER_CONTAINER, PublicAccessType.None);
//				await CreateContainerIfNotExistsAsync(SUBTITLE_CONTAINER, PublicAccessType.None);
//			}
//			catch (Exception ex)
//			{
//				_logger.LogError(ex, "❌ Lỗi tạo Containers khởi tạo.");
//			}
//		}

//		private async Task CreateContainerIfNotExistsAsync(string containerName, PublicAccessType accessType = PublicAccessType.None)
//		{
//			if (_blobServiceClient == null) return;
//			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
//			await containerClient.CreateIfNotExistsAsync(accessType);
//		}

//		public async Task<string> UploadAsync(IFormFile file, string containerName, string? customFileName = null)
//		{
//			if (_blobServiceClient == null) throw new InvalidOperationException("Azure chưa được cấu hình.");

//			var fileName = customFileName ?? (Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));
//			var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
//			var blobClient = containerClient.GetBlobClient(fileName);

//			using var stream = file.OpenReadStream();
//			await blobClient.UploadAsync(stream, new BlobUploadOptions
//			{
//				HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(Path.GetExtension(file.FileName)) }
//			});

//			return blobClient.Uri.ToString();
//		}

//		public async Task<string> GetStreamingUrlAsync(string blobUrl, int expiryHours = 24)
//		{
//			if (_blobServiceClient == null || string.IsNullOrEmpty(blobUrl)) return blobUrl;

//			try
//			{
//				var uri = new Uri(blobUrl);
//				var blobUriBuilder = new BlobUriBuilder(uri);
//				var containerClient = _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName);
//				var blobClient = containerClient.GetBlobClient(blobUriBuilder.BlobName);

//				if (!blobClient.CanGenerateSasUri) return blobUrl;

//				bool isHls = blobUriBuilder.BlobName.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);

//				var sasBuilder = new BlobSasBuilder
//				{
//					BlobContainerName = blobUriBuilder.BlobContainerName,
//					BlobName = isHls ? "" : blobUriBuilder.BlobName,
//					Resource = isHls ? "c" : "b",
//					StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
//					ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiryHours)
//				};

//				if (isHls)
//				{
//					sasBuilder.SetPermissions(BlobContainerSasPermissions.Read);
//				}
//				else
//				{
//					sasBuilder.SetPermissions(BlobSasPermissions.Read);
//				}

//				if (isHls)
//				{
//					var sasUri = containerClient.GenerateSasUri(sasBuilder);
//					return $"{containerClient.Uri}/{blobUriBuilder.BlobName}{sasUri.Query}";
//				}

//				return blobClient.GenerateSasUri(sasBuilder).ToString();
//			}
//			catch
//			{
//				return blobUrl;
//			}
//		}

//		public async Task<bool> DeleteAsync(string blobUrl)
//		{
//			if (_blobServiceClient == null || string.IsNullOrEmpty(blobUrl)) return false;
//			try
//			{
//				var uri = new Uri(blobUrl);
//				var blobUriBuilder = new BlobUriBuilder(uri);
//				return await _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName)
//											  .GetBlobClient(blobUriBuilder.BlobName).DeleteIfExistsAsync();
//			}
//			catch { return false; }
//		}

//		public async Task<bool> ExistsAsync(string blobUrl)
//		{
//			if (_blobServiceClient == null || string.IsNullOrEmpty(blobUrl)) return false;
//			try
//			{
//				var uri = new Uri(blobUrl);
//				var blobUriBuilder = new BlobUriBuilder(uri);
//				return await _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName)
//											  .GetBlobClient(blobUriBuilder.BlobName).ExistsAsync();
//			}
//			catch { return false; }
//		}

//		public async Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null)
//		{
//			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
//			var fileName = episodeNumber.HasValue
//				? $"{movieSlug}/episodes/ep{episodeNumber:D3}-{DateTime.UtcNow:yyyyMMdd}{extension}"
//				: $"{movieSlug}/movie-{DateTime.UtcNow:yyyyMMdd}{extension}";
//			return await UploadAsync(file, VIDEO_CONTAINER, fileName);
//		}

//		public async Task<string> UploadPosterAsync(IFormFile file, string movieSlug)
//		{
//			return await UploadAsync(file, POSTER_CONTAINER, $"{movieSlug}/poster-{DateTime.UtcNow:yyyyMMdd}{Path.GetExtension(file.FileName)}");
//		}

//		public async Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language)
//		{
//			return await UploadAsync(file, SUBTITLE_CONTAINER, $"{movieSlug}/subs/{language}-{DateTime.UtcNow:yyyyMMdd}{Path.GetExtension(file.FileName)}");
//		}

//		public async Task<string> UploadStreamAsync(Stream stream, string fileName, string folderPath)
//		{
//			if (_blobServiceClient == null) throw new InvalidOperationException("Azure chưa cấu hình.");
//			var blobName = string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath.TrimEnd('/')}/{fileName}";
//			var blobClient = _blobServiceClient.GetBlobContainerClient(VIDEO_CONTAINER).GetBlobClient(blobName);

//			await blobClient.UploadAsync(stream, new BlobUploadOptions
//			{
//				HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(Path.GetExtension(fileName)) }
//			});
//			return blobClient.Uri.ToString();
//		}

//		private string GetContentType(string ext) => ext.ToLowerInvariant() switch
//		{
//			".mp4" => "video/mp4",
//			".m3u8" => "application/x-mpegURL",
//			".jpg" or ".jpeg" => "image/jpeg",
//			".png" => "image/png",
//			_ => "application/octet-stream"
//		};

//		public Task DeleteFileAsync(string fileUrl) => DeleteAsync(fileUrl);

//		public async Task<BlobMetadata> GetMetadataAsync(string blobUrl)
//		{
//			if (_blobServiceClient == null) return new BlobMetadata { Url = blobUrl };
//			try
//			{
//				var uri = new Uri(blobUrl);
//				var blobUriBuilder = new BlobUriBuilder(uri);
//				var properties = await _blobServiceClient.GetBlobContainerClient(blobUriBuilder.BlobContainerName)
//													   .GetBlobClient(blobUriBuilder.BlobName).GetPropertiesAsync();
//				return new BlobMetadata
//				{
//					Url = blobUrl,
//					FileName = blobUriBuilder.BlobName,
//					FileSize = properties.Value.ContentLength,
//					ContentType = properties.Value.ContentType,
//					LastModified = properties.Value.LastModified.DateTime
//				};
//			}
//			catch { return new BlobMetadata { Url = blobUrl }; }
//		}
//	}
//}