using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IMovieService
	{
		Task<IEnumerable<MovieDto>> GetAllAsync();
		Task<MovieDto?> GetByIdAsync(Guid id);
		Task<MovieDto?> GetBySlugAsync(string slug); 
		Task<MovieDto> CreateAsync(MovieCreateDto dto);
		Task<bool> UpdateAsync(MovieUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);
		Task<bool> IncrementViewCountAsync(Guid id); 
		Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12); 
		Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12);
		Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId);
		//Task<MovieDto?> UpdateAsync(Guid id, UpdateMovieDto dto);
	}
}