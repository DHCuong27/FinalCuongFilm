using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface ICountryService
	{
		Task<IEnumerable<CountryDto>> GetAllAsync();
		Task<CountryDto?> GetByIdAsync(Guid id);
		Task<CountryDto> CreateAsync(CountryCreateDto dto);
		Task<bool> UpdateAsync(CountryUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);

		Task<PagedResult<CountryDto>> GetPagedAsync(int page = 1, int pageSize = 10);
	}
}