using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IMediaFileService
	{
		Task<IEnumerable<MediaFileDto>> GetAllAsync();
		Task<IEnumerable<MediaFileDto>> GetByMovieIdAsync(Guid movieId);
		Task<IEnumerable<MediaFileDto>> GetByEpisodeIdAsync(Guid episodeId);
		Task<MediaFileDto?> GetByIdAsync(Guid id);
		Task<MediaFileDto> CreateAsync(MediaFileCreateDto dto);
		Task<MediaFileDto> UploadAsync(MediaFileUploadDto dto);
		Task<bool> UpdateAsync(MediaFileUpdateDto dto);
		Task<bool> DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);
	}
}