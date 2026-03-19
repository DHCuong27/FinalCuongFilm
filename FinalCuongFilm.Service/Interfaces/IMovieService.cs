using FinalCuongFilm.Common.DTOs;

public interface IMovieService
{
	Task<IEnumerable<MovieDto>> GetAllAsync();
	Task<MovieDto?> GetByIdAsync(Guid id);
	Task<MovieDto?> GetBySlugAsync(string slug);

	Task<MovieDto> CreateAsync(MovieCreateDto dto);
	Task<MovieDto?> UpdateAsync(Guid id, MovieUpdateDto dto);
	Task<bool> DeleteAsync(Guid id);
	Task<bool> ExistsAsync(Guid id);

	Task IncrementViewCountAsync(Guid movieId);

	Task<IEnumerable<MovieDto>> GetLatestAsync(int count = 12);
	Task<IEnumerable<MovieDto>> GetPopularAsync(int count = 12);

	Task<IEnumerable<MovieDto>> GetByGenreAsync(Guid genreId);
	Task<IEnumerable<MovieDto>> GetByCountryAsync(Guid countryId);

	Task<IEnumerable<MovieDto>> SearchAsync(string keyword);
	Task<PagedResult<MovieDto>> GetPagedAsync(int page = 1, int pageSize = 10);
}