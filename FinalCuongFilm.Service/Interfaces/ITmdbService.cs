using System.Threading.Tasks;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface ITmdbService
	{
		Task<TmdbMovieDto?> SearchMovieAsync(string title);
		Task<TmdbCreditsResponse?> GetMovieCreditsAsync(long tmdbId);
		Task<TmdbMovieDetailsResponse?> GetMovieDetailsAsync(long tmdbId);
	}
}