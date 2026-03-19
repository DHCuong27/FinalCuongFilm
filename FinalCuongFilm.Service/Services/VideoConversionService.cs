using FinalCuongFilm.Service.Interfaces;
using Xabe.FFmpeg;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Logging;
using System;

namespace FinalCuongFilm.Service.Services
{
	public class VideoConversionService : IVideoConversionService
	{
		private readonly IAzureBlobService _azureBlobService;
		private readonly IMediaFileService _mediaFileService;
		private readonly ILogger<VideoConversionService> _logger;
		private readonly string _tempPath;

		public VideoConversionService(IAzureBlobService azureBlobService, IMediaFileService mediaFileService, ILogger<VideoConversionService> logger)
		{
			_azureBlobService = azureBlobService;
			_mediaFileService = mediaFileService;
			_logger = logger;

			_tempPath = Path.Combine(Path.GetTempPath(), "TempVideoProcessing");

			if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);
		}

		public async Task<string> ConvertToHlsAsync(string sourceFileUrl, string slug, int episodeNumber)
		{
			_logger.LogInformation($"[START] Processing HLS for: {slug} - Ep: {episodeNumber}");

			// Safely extract filename from URL
			var uri = new Uri(sourceFileUrl);
			string fileName = Path.GetFileName(uri.LocalPath);

			string localInputPath = Path.Combine(_tempPath, fileName);
			string outputFolderName = $"{slug}_ep{episodeNumber}_hls";
			string localOutputDir = Path.Combine(_tempPath, outputFolderName);

			if (!Directory.Exists(localOutputDir)) Directory.CreateDirectory(localOutputDir);

			try
			{
				// 1. Tải file từ Azure về Local
				_logger.LogInformation($"[DOWNLOAD] Đang tải file MP4 gốc từ Azure về máy...");
				using (var httpClient = new HttpClient())
				{
					using (var response = await httpClient.GetAsync(sourceFileUrl, HttpCompletionOption.ResponseHeadersRead))
					{
						response.EnsureSuccessStatusCode();
						using (var fileStream = new FileStream(localInputPath, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							await response.Content.CopyToAsync(fileStream);
						}
					}
				}

				// 2. Transcode bằng FFmpeg
				_logger.LogInformation($"[FFMPEG] Bắt đầu chia nhỏ và nén Video đa phân giải...");
				string ffmpegArgs =
	$"-i \"{localInputPath}\" " +
	"-map 0:v:0 -map 0:v:0 -map 0:v:0 -map 0:a:0 -map 0:a:0 -map 0:a:0 " +

	// Thêm -preset ultrafast vào từng luồng
	"-s:v:0 1920x1080 -c:v:0 libx264 -preset ultrafast -b:v:0 3000k -profile:v:0 main " +
	"-s:v:1 1280x720 -c:v:1 libx264 -preset ultrafast -b:v:1 1500k -profile:v:1 main " +
	"-s:v:2 854x480 -c:v:2 libx264 -preset ultrafast -b:v:2 800k -profile:v:2 main " +

	"-c:a aac -b:a 128k " +
	"-var_stream_map \"v:0,a:0,name:1080p v:1,a:1,name:720p v:2,a:2,name:480p\" " +
	"-hls_time 10 -hls_list_size 0 -master_pl_name master.m3u8 " +
	$"-f hls \"{localOutputDir}/%v/playlist.m3u8\"";

				var conversion = FFmpeg.Conversions.New().AddParameter(ffmpegArgs);
				await conversion.Start();

				// 3. Upload lên Azure theo luồng có kiểm soát (Chống sập mạng)
				_logger.LogInformation($"[UPLOAD] Bắt đầu đẩy hàng loạt file .ts và .m3u8 lên Azure...");

				string azureFolder = $"movies/{slug}/ep{episodeNumber}/hls";

				var allFiles = Directory.GetFiles(localOutputDir, "*.*", SearchOption.AllDirectories);

				if (allFiles.Length == 0)
				{
					throw new Exception("Lỗi: FFmpeg chạy xong nhưng không sinh ra được file nào!");
				}

				// Giới hạn chỉ cho upload tối đa 10 file cùng lúc
				using var semaphore = new SemaphoreSlim(10);

				var uploadTasks = allFiles.Select(async filePath =>
				{
					await semaphore.WaitAsync(); // Chờ đến lượt
					try
					{
						string relativePath = Path.GetRelativePath(localOutputDir, filePath).Replace("\\", "/");
						using var stream = File.OpenRead(filePath);
						string uploadedUrl = await _azureBlobService.UploadStreamAsync(stream, relativePath, azureFolder);
						return new { Path = relativePath, Url = uploadedUrl };
					}
					finally
					{
						semaphore.Release(); // Xong việc thì nhường chỗ cho file khác
					}
				});

				var uploadResults = await Task.WhenAll(uploadTasks);

				string masterUrl = uploadResults.FirstOrDefault(r => r.Path == "master.m3u8")?.Url ?? string.Empty;

				if (string.IsNullOrEmpty(masterUrl))
				{
					throw new Exception("Lỗi: Không tìm thấy link file master.m3u8 sau khi upload.");
				}

				_logger.LogInformation($"[SUCCESS] Hoàn tất xử lý HLS! Link Master: {masterUrl}");
				return masterUrl;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[ERROR] Lỗi ngầm trong quá trình ConvertToHlsAsync: {ex.Message}");
				throw; // Ném lỗi ra ngoài để Admin biết
			}
			finally
			{
				// Dọn dẹp ổ đĩa cục bộ
				if (File.Exists(localInputPath)) File.Delete(localInputPath);
				if (Directory.Exists(localOutputDir)) Directory.Delete(localOutputDir, true);
			}
		}

		// Trong VideoConversionService.cs thêm hàm này:
		public async Task ProcessVideoBackgroundJobAsync(Guid mediaFileId, string mp4Url, string slug, int episodeNumber)
		{
			try
			{
				// 1. Chạy hàm convert nặng nề
				string masterM3u8Url = await ConvertToHlsAsync(mp4Url, slug, episodeNumber);

				// 2. Cập nhật Database sau khi xong
				var mediaFile = await _mediaFileService.GetByIdAsync(mediaFileId);
				if (mediaFile != null)
				{
					// FIX LỖI: Tạo một đối tượng UpdateDto mới và copy dữ liệu cũ sang, kèm theo dữ liệu mới
					var updateDto = new FinalCuongFilm.Common.DTOs.MediaFileUpdateDto
					{
						Id = mediaFile.Id,
						FileName = mediaFile.FileName,
						FileUrl = masterM3u8Url, // Cập nhật link HLS mới
						FileType = "hls",        // Đổi định dạng thành hls
						Quality = "Auto",        // Đổi trạng thái từ "Processing..." thành "Auto"
						Language = mediaFile.Language,
						FileSizeBytes = mediaFile.FileSizeBytes,
						MovieId = mediaFile.MovieId,
						EpisodeId = mediaFile.EpisodeId
					};

					// Lưu bản Update này vào Database
					await _mediaFileService.UpdateAsync(updateDto);

					_logger.LogInformation($"[HANGFIRE] Đã cập nhật xong file HLS vào Database cho MediaId: {mediaFileId}");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[HANGFIRE] Lỗi khi xử lý ngầm video cho MediaId: {mediaFileId}");
				// Nếu cần, bạn có thể tạo updateDto để set Quality = "Lỗi Convert" ở đây
			}
		}
	}
}