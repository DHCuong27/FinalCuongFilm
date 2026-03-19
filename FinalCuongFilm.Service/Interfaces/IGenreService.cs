using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IGenreService
	{
		Task<IEnumerable<GenreDto>> GetAllAsync();
		Task<GenreDto?> GetByIdAsync(Guid id);
		Task<GenreDto> CreateAsync(GenreCreateDto dto);
		Task<bool> UpdateAsync(GenreUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);

		Task<PagedResult<GenreDto>> GetPagedAsync(int page = 1, int pageSize = 10);
	}
}