using AutoMapper;
using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.Service.Services
{
	public class EpisodeService : IEpisodeService
	{
		private readonly CuongFilmDbContext _context;
		private readonly IMapper _mapper;

		public EpisodeService(CuongFilmDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public async Task<IEnumerable<EpisodeDto>> GetAllAsync()
		{
			var episodes = await _context.Episodes
				.Include(e => e.Movie)
				.ToListAsync();

			return episodes.Select(e => MapToDto(e)).ToList();
		}

		public async Task<IEnumerable<EpisodeDto>> GetByMovieIdAsync(Guid movieId)
		{
			var episodes = await _context.Episodes
				.Include(e => e.Movie)
				.Where(e => e.MovieId == movieId)
				.OrderBy(e => e.EpisodeNumber)
				.ToListAsync();

			return episodes.Select(e => MapToDto(e)).ToList();
		}

		public async Task<EpisodeDto?> GetByIdAsync(Guid id)
		{
			var episode = await _context.Episodes
				.Include(e => e.Movie)
				.FirstOrDefaultAsync(e => e.Id == id);

			return episode == null ? null : MapToDto(episode);
		}

		public async Task<EpisodeDto> CreateAsync(EpisodeCreateDto dto)
		{
			var episode = new Episode
			{
				Id = Guid.NewGuid(),
				EpisodeNumber = dto.EpisodeNumber,
				Title = dto.Title,
				Description = dto.Description,
				DurationMinutes = dto.DurationMinutes,
				AirDate = dto.AirDate,
				IsActive = dto.IsActive,
				MovieId = dto.MovieId,
				ViewCount = 0
			};

			_context.Episodes.Add(episode);
			await _context.SaveChangesAsync();

			return await GetByIdAsync(episode.Id) ?? throw new Exception("Failed to create episode.");
		}

		public async Task<bool> UpdateAsync(EpisodeUpdateDto dto)
		{
			var episode = await _context.Episodes.FindAsync(dto.Id);
			if (episode == null) return false;

			episode.EpisodeNumber = dto.EpisodeNumber;
			episode.Title = dto.Title;
			episode.Description = dto.Description;
			episode.DurationMinutes = dto.DurationMinutes;
			episode.AirDate = dto.AirDate;
			episode.IsActive = dto.IsActive;

			// STRICT BUSINESS LOGIC: 
			// We intentionally do NOT map `episode.MovieId = dto.MovieId`. 
			// An episode's parent series is immutable to prevent data corruption.

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var episode = await _context.Episodes
				.Include(e => e.MediaFiles)
				.FirstOrDefaultAsync(e => e.Id == id);

			if (episode == null) return false;

			if (episode.MediaFiles.Any())
			{
				throw new InvalidOperationException("Cannot delete this episode because it has associated media files. Please delete all media files first.");
			}

			_context.Episodes.Remove(episode);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ExistsAsync(Guid id)
		{
			return await _context.Episodes.AnyAsync(e => e.Id == id);
		}

		// FIX: Replaced generic 'searchString' with 'movieId' to correctly filter episodes by their parent Series
		public async Task<PagedResult<EpisodeDto>> GetPagedAsync(Guid? movieId = null, int pageIndex = 1, int pageSize = 10)
		{
			if (pageIndex < 1) pageIndex = 1;
			if (pageSize < 1) pageSize = 10;

			var query = _context.Episodes
				.Include(e => e.Movie)
				.AsQueryable();

			if (movieId.HasValue)
			{
				query = query.Where(e => e.MovieId == movieId.Value);
			}

			int totalCount = await query.CountAsync();

			var items = await query.OrderBy(e => e.EpisodeNumber)
								   .Skip((pageIndex - 1) * pageSize)
								   .Take(pageSize)
								   .ToListAsync();

			var dtos = _mapper.Map<List<EpisodeDto>>(items);

			return new PagedResult<EpisodeDto>
			{
				Items = dtos,
				TotalCount = totalCount,
				PageIndex = pageIndex,
				PageSize = pageSize
			};
		}

		private static EpisodeDto MapToDto(Episode episode)
		{
			return new EpisodeDto
			{
				Id = episode.Id,
				EpisodeNumber = episode.EpisodeNumber,
				Title = episode.Title,
				Description = episode.Description,
				DurationMinutes = episode.DurationMinutes,
				AirDate = episode.AirDate,
				ViewCount = episode.ViewCount,
				IsActive = episode.IsActive,
				MovieId = episode.MovieId,
				MovieTitle = episode.Movie?.Title
			};
		}
	}
}