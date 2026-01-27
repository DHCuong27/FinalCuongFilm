using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IMovieService
	{
		Task<IEnumerable<MovieDto>> GetAllAsync();
		Task<MovieDto?> GetByIdAsync(Guid id);
		Task<MovieDto> CreateAsync(MovieCreateDto dto);
		Task<bool> UpdateAsync(MovieUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);
	}
}