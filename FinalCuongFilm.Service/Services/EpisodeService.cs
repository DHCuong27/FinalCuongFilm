//using FinalCuongFilm.ApplicationCore.Entities;
//using FinalCuongFilm.Common.DTOs;
//using FinalCuongFilm.Datalayer;
//using FinalCuongFilm.DataLayer;
//using FinalCuongFilm.Service.Interfaces;
//using Microsoft.EntityFrameworkCore;

//namespace FinalCuongFilm.Service.Services
//{
//	public class EpisodeService : IEpisodeService
//	{
//		private readonly CuongFilmDbContext _context;

//		public EpisodeService(CuongFilmDbContext context)
//		{
//			_context = context;
//		}

//		public async Task<IEnumerable<EpisodeDto>> GetAllAsync()
//		{
//			var episodes = await _context.Episodes
//				.Include(e => e.Movie)
//				.ToListAsync();

//			return episodes.Select(e => MapToDto(e)).ToList();
//		}

//		public async Task<IEnumerable<EpisodeDto>> GetByMovieIdAsync(Guid movieId)
//		{
//			var episodes = await _context.Episodes
//				.Include(e => e.Movie)
//				.Where(e => e.MovieId == movieId)
//				.OrderBy(e => e.EpisodeNumber)
//				.ToListAsync();

//			return episodes.Select(e => MapToDto(e)).ToList();
//		}

//		public async Task<EpisodeDto?> GetByIdAsync(Guid id)
//		{
//			var episode = await _context.Episodes
//				.Include(e => e.Movie)
//				.FirstOrDefaultAsync(e => e.Id == id);

//			return episode == null ? null : MapToDto(episode);
//		}

//		public async Task<EpisodeDto> CreateAsync(EpisodeCreateDto dto)
//		{
//			var episode = new Episode
//			{
//				Id = Guid.NewGuid(),
//				EpisodeNumber = dto.EpisodeNumber,
//				Title = dto.Title,
//				Description = dto.Description,
//				DurationMinutes = dto.DurationMinutes,
//				AirDate = dto.AirDate,
//				IsActive = dto.IsActive,
//				MovieId = dto.MovieId,
//				ViewCount = 0
//			};

//			_context.Episodes.Add(episode);
//			await _context.SaveChangesAsync();

//			return await GetByIdAsync(episode.Id) ?? throw new Exception("Failed to create episode");
//		}

//		public async Task<bool> UpdateAsync(EpisodeUpdateDto dto)
//		{
//			var episode = await _context.Episodes.FindAsync(dto.Id);
//			if (episode == null)
//				return false;

//			episode.EpisodeNumber = dto.EpisodeNumber;
//			episode.Title = dto.Title;
//			episode.Description = dto.Description;
//			episode.DurationMinutes = dto.DurationMinutes;
//			episode.AirDate = dto.AirDate;
//			episode.IsActive = dto.IsActive;
//			episode.MovieId = dto.MovieId;

//			await _context.SaveChangesAsync();
//			return true;
//		}

//		public async Task<bool> DeleteAsync(Guid id)
//		{
//			var episode = await _context.Episodes
//				.Include(e => e.MediaFiles)
//				.FirstOrDefaultAsync(e => e.Id == id);

//			if (episode == null)
//				return false;

//			// Kiểm tra nghiệp vụ: không cho xóa nếu có media files
//			if (episode.MediaFiles.Any())
//			{
//				throw new InvalidOperationException("Không thể xóa tập phim đã có media files. Vui lòng xóa tất cả media files trước.");
//			}

//			_context.Episodes.Remove(episode);
//			await _context.SaveChangesAsync();
//			return true;
//		}

//		public async Task<bool> ExistsAsync(Guid id)
//		{
//			return await _context.Episodes.AnyAsync(e => e.Id == id);
//		}

//		private static EpisodeDto MapToDto(Episode episode)
//		{
//			return new EpisodeDto
//			{
//				Id = episode.Id,
//				EpisodeNumber = episode.EpisodeNumber,
//				Title = episode.Title,
//				Description = episode.Description,
//				DurationMinutes = episode.DurationMinutes,
//				AirDate = episode.AirDate,
//				ViewCount = episode.ViewCount,
//				IsActive = episode.IsActive,
//				MovieId = episode.MovieId,
//				MovieTitle = episode.Movie?.Title
//			};
//		}
//	}
//}