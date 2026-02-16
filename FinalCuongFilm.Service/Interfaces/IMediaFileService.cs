using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
    public interface IMediaFileService
    {
        Task<IEnumerable<MediaFileDto>> GetAllAsync();
        Task<MediaFileDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<MediaFileDto>> GetByMovieIdAsync(Guid movieId);
        Task<IEnumerable<MediaFileDto>> GetByEpisodeIdAsync(Guid episodeId);
        Task<MediaFileDto?> GetSubtitlesAsync(Guid mediaFileId, string language);
        Task<MediaFileDto> CreateAsync(MediaFileCreateDto dto);
        Task<bool> UpdateAsync(MediaFileUpdateDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}