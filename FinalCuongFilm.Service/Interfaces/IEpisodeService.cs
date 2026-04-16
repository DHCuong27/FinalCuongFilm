using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IEpisodeService
	{
		Task<IEnumerable<EpisodeDto>> GetAllAsync();

		Task<IEnumerable<EpisodeDto>> GetByMovieIdAsync(Guid movieId);

		Task<EpisodeDto?> GetByIdAsync(Guid id);

		Task<EpisodeDto> CreateAsync(EpisodeCreateDto dto);

		Task<bool> UpdateAsync(EpisodeUpdateDto dto);

		Task<bool> DeleteAsync(Guid id);

		Task<bool> ExistsAsync(Guid id);

		Task<PagedResult<EpisodeDto>> GetPagedAsync(Guid? movieId = null, int pageIndex = 1, int pageSize = 10);
	}
}