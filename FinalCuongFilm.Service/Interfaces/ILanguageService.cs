using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface ILanguageService
	{
		Task<IEnumerable<LanguageDto>> GetAllAsync();
		Task<LanguageDto?> GetByIdAsync(Guid id);
		Task<LanguageDto> CreateAsync(LanguageCreateDto dto);
		Task<bool> UpdateAsync(LanguageUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);
	}
}