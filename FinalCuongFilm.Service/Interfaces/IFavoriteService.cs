using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IFavoriteService
	{
		Task<IEnumerable<FavoriteDto>> GetUserFavoritesAsync(string userId);
		Task<bool> IsFavoriteAsync(string userId, Guid movieId);
		Task<FavoriteDto> AddFavoriteAsync(string userId, Guid movieId);
		Task<bool> RemoveFavoriteAsync(string userId, Guid movieId);
		Task<int> GetFavoriteCountAsync(Guid movieId);
	}
}