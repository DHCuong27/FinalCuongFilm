using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Services;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IFavoriteService
	{
		Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId);
		Task<bool> IsFavoriteAsync(string userId, Guid movieId);
		Task<FavoriteDto> AddFavoriteAsync(string userId, Guid movieId);
		Task<bool> RemoveFavoriteAsync(string userId, Guid movieId);
		Task<int> GetFavoriteCountAsync(Guid movieId);

		Task<IEnumerable<MovieDto>> GetUserWatchHistoryAsync(string userId);
		Task SaveWatchHistoryAsync(string userId, Guid movieId);


	}
}