using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IActorService
	{
		Task<IEnumerable<ActorDto>> GetAllAsync();
		Task<ActorDto?> GetByIdAsync(Guid id);
		Task<ActorDto> CreateAsync(ActorCreateDto dto);
		Task<bool> UpdateAsync(ActorUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);

		Task<PagedResult<ActorDto>> GetPagedAsync(string? searchString = null, int pageIndex = 1, int pageSize = 10);
	}
}