using System.Threading.Tasks;
using FinalCuongFilm.Common.DTOs;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface ITmdbService
	{
		// Movie
		Task<TmdbMovieDto?> SearchMovieAsync(string title);
		Task<TmdbCreditsResponse?> GetMovieCreditsAsync(long tmdbId);
		Task<TmdbMovieDetailsResponse?> GetMovieDetailsAsync(long tmdbId);

		// TV Series 
		Task<TmdbMovieDto?> SearchTvShowAsync(string title);
		Task<TmdbMovieDetailsResponse?> GetTvShowDetailsAsync(long tmdbId);
		Task<TmdbCreditsResponse?> GetTvCreditsAsync(long tmdbId);
	}
}