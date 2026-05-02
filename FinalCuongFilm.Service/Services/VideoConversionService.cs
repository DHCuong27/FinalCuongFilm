using FinalCuongFilm.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

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

		// Bổ sung CancellationToken vào hàm
		public async Task<string> ConvertToHlsAsync(string sourceFileUrl, string slug, int episodeNumber, CancellationToken cancellationToken)
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
				_logger.LogInformation($"[DOWNLOAD] Downloading the original MP4 file from Azure to my computer....");
				using (var httpClient = new HttpClient())
				{
					// Chèn token vào GetAsync để có thể hủy tải file nếu cần
					using (var response = await httpClient.GetAsync(sourceFileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
					{
						response.EnsureSuccessStatusCode();
						using (var fileStream = new FileStream(localInputPath, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							// Chèn token vào CopyToAsync
							await response.Content.CopyToAsync(fileStream, cancellationToken);
						}
					}
				}

				// 2. Transcode bằng FFmpeg
				_logger.LogInformation($"[FFMPEG] Start splitting and compressing multi-resolution video....");
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

				// 🔥 ĐIỂM CHỐT HẠ: Truyền token vào Start(). 
				// Khi Hangfire hủy Job, lệnh này sẽ bắt Xabe.FFmpeg ép kill tiến trình ffmpeg.exe
				await conversion.Start(cancellationToken);

				// 3. Upload video lên Azure 
				_logger.LogInformation($"[UPLOAD] Start pushing .ts and .m3u8 files to Azure in bulk....");

				string azureFolder = $"movies/{slug}/ep{episodeNumber}/hls";

				var allFiles = Directory.GetFiles(localOutputDir, "*.*", SearchOption.AllDirectories);

				if (allFiles.Length == 0)
				{
					throw new Exception("Error: FFmpeg finished running but failed to create any files!");
				}

				// Giới hạn chỉ cho upload tối đa 10 file cùng lúc
				using var semaphore = new SemaphoreSlim(10);

				var uploadTasks = allFiles.Select(async filePath =>
				{
					// Dừng việc up file lại ngay nếu có lệnh hủy Job
					await semaphore.WaitAsync(cancellationToken);
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
					throw new Exception("Error: The master.m3u8 file link could not be found after uploading.");
				}

				_logger.LogInformation($"[SUCCESS] HLS processing complete! Link Master: {masterUrl}");
				return masterUrl;
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning($"[CANCELLED] HLS processing was explicitly cancelled by Hangfire for: {slug}");
				throw; // Phải throw ra để Hangfire nhận diện trạng thái Canceled/Failed
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[ERROR] Hidden error during ConvertToHlsAsync: {ex.Message}");
				throw;
			}
			finally
			{
				// Đoạn này rất quan trọng, giữ nguyên để luôn dọn rác dù thành công hay bị hủy ngang
				if (File.Exists(localInputPath)) File.Delete(localInputPath);
				if (Directory.Exists(localOutputDir)) Directory.Delete(localOutputDir, true);
			}
		}

		// Bổ sung CancellationToken vào tham số của Hangfire Job
		public async Task ProcessVideoBackgroundJobAsync(Guid mediaFileId, string mp4Url, string slug, int episodeNumber, CancellationToken cancellationToken)
		{
			try
			{
				// 1. Chạy hàm convert (Chuyền token xuống dưới)
				string masterM3u8Url = await ConvertToHlsAsync(mp4Url, slug, episodeNumber, cancellationToken);

				// 2. Cập nhật Database sau khi xong
				var mediaFile = await _mediaFileService.GetByIdAsync(mediaFileId);
				if (mediaFile != null)
				{
					var updateDto = new FinalCuongFilm.Common.DTOs.MediaFileUpdateDto
					{
						Id = mediaFile.Id,
						FileName = mediaFile.FileName,
						FileUrl = masterM3u8Url,
						FileType = "hls",
						Quality = "Auto",
						Language = mediaFile.Language,
						FileSizeBytes = mediaFile.FileSizeBytes,
						MovieId = mediaFile.MovieId,
						EpisodeId = mediaFile.EpisodeId
					};

					// Lưu bản Update này vào Database
					await _mediaFileService.UpdateAsync(updateDto);

					_logger.LogInformation($"[HANGFIRE] The HLS file has been updated in the database for MediaId: {mediaFileId}");
				}
			}
			catch (OperationCanceledException)
			{
				_logger.LogWarning($"[HANGFIRE] Job cancelled for MediaId: {mediaFileId}");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[HANGFIRE] Error when processing video in the background for MediaId: {mediaFileId}");
				throw; // Ném lỗi ra ngoài để Hangfire Dashboard đổi sang màu đỏ (Failed)
			}
		}
	}
}