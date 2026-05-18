using System.Net.Http.Headers;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinalCuongFilm.Service.Services
{
	public class SupabaseStorageService : IStorageService
	{
		private readonly HttpClient _httpClient;
		private readonly ILogger<SupabaseStorageService> _logger;
		private readonly string _supabaseUrl;
		private readonly string _serviceRoleKey;

		private const string VIDEO_BUCKET = "videos";
		private const string POSTER_BUCKET = "posters";
		private const string SUBTITLE_BUCKET = "subtitles";

		public SupabaseStorageService(
			IHttpClientFactory httpClientFactory,
			IConfiguration configuration,
			ILogger<SupabaseStorageService> logger)
		{
			_httpClient = httpClientFactory.CreateClient();
			_logger = logger;

			_supabaseUrl = configuration["SUPABASE_URL"]
				?? throw new InvalidOperationException("SUPABASE_URL is missing");
			_serviceRoleKey = configuration["SUPABASE_SERVICE_ROLE_KEY"]
				?? throw new InvalidOperationException("SUPABASE_SERVICE_ROLE_KEY is missing");

			// Optional: tăng timeout để upload file lớn
			_httpClient.Timeout = TimeSpan.FromMinutes(30);
		}

		private void ApplyAuthHeaders(HttpRequestMessage req)
		{
			req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
			req.Headers.Add("apikey", _serviceRoleKey);
		}

		private string PublicUrl(string bucket, string path)
			=> $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucket}/{path}";

		public async Task<string> UploadAsync(IFormFile file, string bucketName, string? customFileName = null)
		{
			var fileName = customFileName ?? (Guid.NewGuid() + Path.GetExtension(file.FileName));
			using var stream = file.OpenReadStream();
			return await UploadStreamInternalAsync(stream, fileName, bucketName, file.ContentType);
		}

		public async Task<string> UploadStreamAsync(Stream stream, string fileName, string folderPath)
		{
			var path = string.IsNullOrEmpty(folderPath) ? fileName : $"{folderPath.TrimEnd('/')}/{fileName}";
			return await UploadStreamInternalAsync(stream, path, VIDEO_BUCKET, GetContentType(Path.GetExtension(fileName)));
		}

		private async Task<string> UploadStreamInternalAsync(Stream stream, string path, string bucket, string contentType)
		{
			path = path.TrimStart('/');
			var safePath = Uri.EscapeDataString(path).Replace("%2F", "/");

			var url = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/{bucket}/{safePath}";
			using var req = new HttpRequestMessage(HttpMethod.Post, url);

			ApplyAuthHeaders(req);
			req.Headers.Add("x-upsert", "true");

			var content = new StreamContent(stream);
			content.Headers.ContentType = new MediaTypeHeaderValue(
				string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
			req.Content = content;

			var resp = await _httpClient.SendAsync(req);
			if (!resp.IsSuccessStatusCode)
			{
				var body = await resp.Content.ReadAsStringAsync();
				throw new Exception($"Supabase upload failed: {resp.StatusCode} - {body}");
			}

			return PublicUrl(bucket, safePath);
		}

		public Task<string> UploadVideoAsync(IFormFile file, string movieSlug, int? episodeNumber = null)
		{
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			var fileName = episodeNumber.HasValue
				? $"{movieSlug}/episodes/ep{episodeNumber:D3}-{DateTime.UtcNow:yyyyMMdd}{extension}"
				: $"{movieSlug}/movie-{DateTime.UtcNow:yyyyMMdd}{extension}";
			return UploadAsync(file, VIDEO_BUCKET, fileName);
		}

		public Task<string> UploadPosterAsync(IFormFile file, string movieSlug)
			=> UploadAsync(file, POSTER_BUCKET, $"{movieSlug}/poster-{DateTime.UtcNow:yyyyMMdd}{Path.GetExtension(file.FileName)}");

		public Task<string> UploadSubtitleAsync(IFormFile file, string movieSlug, string language)
			=> UploadAsync(file, SUBTITLE_BUCKET, $"{movieSlug}/subs/{language}-{DateTime.UtcNow:yyyyMMdd}{Path.GetExtension(file.FileName)}");

		public Task<string> GetStreamingUrlAsync(string fileUrl, int expiryHours = 24)
		{
			// Bucket public => không cần signed URL
			return Task.FromResult(fileUrl);
		}

		public async Task<bool> DeleteAsync(string fileUrl)
		{
			var (bucket, path) = ParsePublicUrl(fileUrl);
			var url = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/{bucket}/{path}";
			using var req = new HttpRequestMessage(HttpMethod.Delete, url);
			ApplyAuthHeaders(req);
			var resp = await _httpClient.SendAsync(req);
			return resp.IsSuccessStatusCode;
		}

		public Task DeleteFileAsync(string fileUrl) => DeleteAsync(fileUrl);

		public async Task<bool> ExistsAsync(string fileUrl)
		{
			var (bucket, path) = ParsePublicUrl(fileUrl);
			var url = $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/{bucket}/{path}";
			using var req = new HttpRequestMessage(HttpMethod.Head, url);
			ApplyAuthHeaders(req);
			var resp = await _httpClient.SendAsync(req);
			return resp.IsSuccessStatusCode;
		}

		public Task<BlobMetadata> GetMetadataAsync(string fileUrl)
		{
			return Task.FromResult(new BlobMetadata { Url = fileUrl });
		}

		private (string bucket, string path) ParsePublicUrl(string url)
		{
			var marker = "/storage/v1/object/public/";
			var idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			if (idx < 0) throw new InvalidOperationException("Invalid public file URL");
			var rest = url[(idx + marker.Length)..];
			var slash = rest.IndexOf('/');
			if (slash < 0) throw new InvalidOperationException("Invalid public file URL");
			return (rest.Substring(0, slash), rest[(slash + 1)..]);
		}

		private string GetContentType(string ext) => ext.ToLowerInvariant() switch
		{
			".mp4" => "video/mp4",
			".m3u8" => "application/x-mpegURL",
			".ts" => "video/mp2t",
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".webp" => "image/webp",
			".srt" => "application/x-subrip",
			".vtt" => "text/vtt",
			_ => "application/octet-stream"
		};
	}
}