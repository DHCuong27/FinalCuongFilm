using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class MediaFileService : IMediaFileService
	{
		private readonly CuongFilmDbContext _context;

		
			public MediaFileService(CuongFilmDbContext context)
			{
				_context = context;
			}

			public async Task<IEnumerable<MediaFileDto>> GetAllAsync()
		{
			var mediaFiles = await _context.MediaFiles
				.Include(m => m.Movie)
				.Include(m => m.Episode)
				.ToListAsync();

			return mediaFiles.Select(m => MapToDto(m)).ToList();
		}

		public async Task<IEnumerable<MediaFileDto>> GetByMovieIdAsync(Guid movieId)
		{
			var mediaFiles = await _context.MediaFiles
				.Include(m => m.Movie)
				.Include(m => m.Episode)
				.Where(m => m.MovieId == movieId)
				.ToListAsync();

			return mediaFiles.Select(m => MapToDto(m)).ToList();
		}


		public async Task<IEnumerable<MediaFileDto>> GetByEpisodeIdAsync(Guid episodeId)
		{
			var mediaFiles = await _context.MediaFiles
				.Include(m => m.Movie)
				.Include(m => m.Episode)
				.Where(m => m.EpisodeId == episodeId)
				.ToListAsync();

			return mediaFiles.Select(m => MapToDto(m)).ToList();
		}

		public async Task<MediaFileDto?> GetByIdAsync(Guid id)
		{
			var mediaFile = await _context.MediaFiles
				.Include(m => m.Movie)
				.Include(m => m.Episode)
				.FirstOrDefaultAsync(m => m.Id == id);

			return mediaFile == null ? null : MapToDto(mediaFile);
		}

		public async Task<MediaFileDto> CreateAsync(MediaFileCreateDto dto)
		{
			if (!dto.MovieId.HasValue && !dto.EpisodeId.HasValue)
			{
				throw new InvalidOperationException("Must select a Movie or Episode.");
			}

			var mediaFile = new MediaFile
			{
				Id = Guid.NewGuid(),
				FileName = dto.FileName,
				FileUrl = dto.FileUrl,
				FilePath = dto.FilePath,
				FileSizeBytes = dto.FileSizeBytes,
				FileType = dto.FileType,
				Quality = dto.Quality,
				Language = dto.Language,
				MovieId = dto.MovieId,
				EpisodeId = dto.EpisodeId,
				UploadedAt = DateTime.UtcNow
			};

			_context.MediaFiles.Add(mediaFile);
			await _context.SaveChangesAsync();

			return await GetByIdAsync(mediaFile.Id) ?? throw new Exception("Failed to create media file");
		}

		// Thêm vào class MediaFileService
		public async Task<MediaFileDto?> GetSubtitlesAsync(Guid mediaFileId, string language)
		{
			var mediaFile = await _context.MediaFiles.FindAsync(mediaFileId);
			if (mediaFile == null)
				return null;

			// Find subtitle for same movie/episode with specified language
			var subtitle = await _context.MediaFiles
				.Where(m => m.FileType == "subtitle"
					&& m.Language == language
					&& ((m.MovieId == mediaFile.MovieId && m.EpisodeId == null && mediaFile.EpisodeId == null)
						|| (m.EpisodeId == mediaFile.EpisodeId && mediaFile.EpisodeId != null)))
				.FirstOrDefaultAsync();

			return subtitle == null ? null : MapToDto(subtitle);
		}

		public async Task<bool> UpdateAsync(MediaFileUpdateDto dto)
		{
			var mediaFile = await _context.MediaFiles.FindAsync(dto.Id);
			if (mediaFile == null)
				return false;

			mediaFile.FileName = dto.FileName;
			mediaFile.FileUrl = dto.FileUrl;
			mediaFile.FilePath = dto.FilePath;
			mediaFile.FileSizeBytes = dto.FileSizeBytes;
			mediaFile.FileType = dto.FileType;
			mediaFile.Quality = dto.Quality;
			mediaFile.Language = dto.Language;
			mediaFile.MovieId = dto.MovieId;
			mediaFile.EpisodeId = dto.EpisodeId;

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var mediaFile = await _context.MediaFiles.FindAsync(id);
			if (mediaFile == null)
				return false;

			// Xóa file vật lý nếu tồn tại
			if (!string.IsNullOrEmpty(mediaFile.FilePath) && File.Exists(mediaFile.FilePath))
			{
				try
				{
					File.Delete(mediaFile.FilePath);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Could not delete file: {ex.Message}");
				}
			}

			_context.MediaFiles.Remove(mediaFile);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.MediaFiles.AnyAsync(m => m.Id == id);
		}

		private static MediaFileDto MapToDto(MediaFile mediaFile)
		{
			return new MediaFileDto
			{
				Id = mediaFile.Id,
				FileName = mediaFile.FileName,
				FileUrl = mediaFile.FileUrl,
				FilePath = mediaFile.FilePath,
				FileSizeBytes = mediaFile.FileSizeBytes,
				FileType = mediaFile.FileType,
				Quality = mediaFile.Quality,
				Language = mediaFile.Language,
				UploadedAt = mediaFile.UploadedAt,
				MovieId = mediaFile.MovieId,
				MovieTitle = mediaFile.Movie?.Title,
				EpisodeId = mediaFile.EpisodeId,
				EpisodeTitle = mediaFile.Episode?.Title,
				EpisodeNumber = mediaFile.Episode?.EpisodeNumber
			};
		}

	}
}