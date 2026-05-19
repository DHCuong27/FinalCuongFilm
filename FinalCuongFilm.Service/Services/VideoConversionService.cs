using FinalCuongFilm.Service.Interfaces;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;

namespace FinalCuongFilm.Service.Services
{
	public class VideoConversionService : IVideoConversionService
	{
		private readonly IStorageService _storageService;
		private readonly IMediaFileService _mediaFileService;
		private readonly ILogger<VideoConversionService> _logger;
		private readonly string _tempPath;

		public VideoConversionService(IStorageService storageService, IMediaFileService mediaFileService, ILogger<VideoConversionService> logger)
		{
			_storageService = storageService;
			_mediaFileService = mediaFileService;
			_logger = logger;

			_tempPath = Path.Combine(Path.GetTempPath(), "TempVideoProcessing");
			if (!Directory.Exists(_tempPath)) Directory.CreateDirectory(_tempPath);
		}

		public async Task<string> ConvertToHlsAsync(string sourceFileUrl, string slug, int episodeNumber, CancellationToken cancellationToken)
		{
			_logger.LogInformation($"[START] Processing HLS for: {slug} - Ep: {episodeNumber}");

			var uri = new Uri(sourceFileUrl);
			string fileName = Path.GetFileName(uri.LocalPath);

			string localInputPath = Path.Combine(_tempPath, fileName);
			string outputFolderName = $"{slug}_ep{episodeNumber}_hls";
			string localOutputDir = Path.Combine(_tempPath, outputFolderName);

			if (!Directory.Exists(localOutputDir)) Directory.CreateDirectory(localOutputDir);

			try
			{
				_logger.LogInformation($"[DOWNLOAD] Downloading the original MP4 file from storage....");
				using (var httpClient = new HttpClient())
				{
					using (var response = await httpClient.GetAsync(sourceFileUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
					{
						response.EnsureSuccessStatusCode();
						using (var fileStream = new FileStream(localInputPath, FileMode.Create, FileAccess.Write, FileShare.None))
						{
							await response.Content.CopyToAsync(fileStream, cancellationToken);
						}
					}
				}

				_logger.LogInformation($"[FFMPEG] Start splitting and compressing multi-resolution video....");
				string ffmpegArgs =
				$"-i \"{localInputPath}\" " +
				"-map 0:v:0 -map 0:v:0 -map 0:v:0 -map 0:a:0 -map 0:a:0 -map 0:a:0 " +
				"-s:v:0 1920x1080 -c:v:0 libx264 -preset ultrafast -b:v:0 3000k -profile:v:0 main " +
				"-s:v:1 1280x720 -c:v:1 libx264 -preset ultrafast -b:v:1 1500k -profile:v:1 main " +
				"-s:v:2 854x480 -c:v:2 libx264 -preset ultrafast -b:v:2 800k -profile:v:2 main " +
				"-c:a aac -b:a 128k " +
				"-var_stream_map \"v:0,a:0,name:1080p v:1,a:1,name:720p v:2,a:2,name:480p\" " +
				"-hls_time 10 -hls_list_size 0 -master_pl_name master.m3u8 " +
				$"-f hls \"{localOutputDir}/%v/playlist.m3u8\"";

				var conversion = FFmpeg.Conversions.New().AddParameter(ffmpegArgs);
				await conversion.Start(cancellationToken);

				_logger.LogInformation($"[UPLOAD] Start pushing .ts and .m3u8 files to storage in bulk....");

				string storageFolder = $"movies/{slug}/ep{episodeNumber}/hls";

				var allFiles = Directory.GetFiles(localOutputDir, "*.*", SearchOption.AllDirectories);

				if (allFiles.Length == 0)
				{
					throw new Exception("Error: FFmpeg finished running but failed to create any files!");
				}

				using var semaphore = new SemaphoreSlim(10);

				var uploadTasks = allFiles.Select(async filePath =>
				{
					await semaphore.WaitAsync(cancellationToken);
					try
					{
						string relativePath = Path.GetRelativePath(localOutputDir, filePath).Replace("\\", "/");
						using var stream = File.OpenRead(filePath);
						string uploadedUrl = await _storageService.UploadStreamAsync(stream, relativePath, storageFolder);
						return new { Path = relativePath, Url = uploadedUrl };
					}
					finally
					{
						semaphore.Release();
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
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"[ERROR] Hidden error during ConvertToHlsAsync: {ex.Message}");
				throw;
			}
			finally
			{
				if (File.Exists(localInputPath)) File.Delete(localInputPath);
				if (Directory.Exists(localOutputDir)) Directory.Delete(localOutputDir, true);
			}
		}

		public async Task ProcessVideoBackgroundJobAsync(Guid mediaFileId, string mp4Url, string slug, int episodeNumber, CancellationToken cancellationToken)
		{
			try
			{
				string masterM3u8Url = await ConvertToHlsAsync(mp4Url, slug, episodeNumber, cancellationToken);

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
				throw;
			}
		}
	}
}