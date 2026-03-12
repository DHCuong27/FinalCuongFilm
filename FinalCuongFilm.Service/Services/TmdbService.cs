using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using FinalCuongFilm.Service.Interfaces;
using FinalCuongFilm.Common.DTOs;
using Microsoft.Extensions.Configuration; 

namespace FinalCuongFilm.Service.Services
{
	public class TmdbService : ITmdbService
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private readonly string _baseUrl = "https://api.themoviedb.org/3";

		// Inject IConfiguration vào đây
		public TmdbService(HttpClient httpClient, IConfiguration config)
		{
			_httpClient = httpClient;
			// Lấy Key từ file appsettings.json
			_apiKey = config["TmdbSettings:ApiKey"] ?? "NẾU_FILE_JSON_KHÔNG_CÓ_THÌ_DÁN_KEY_CHỮA_CHÁY_VÀO_ĐÂY";
		}

		public async Task<TmdbMovieDto?> SearchMovieAsync(string title)
		{
			var encodedTitle = Uri.EscapeDataString(title);

				var url = $"{_baseUrl}/search/movie?query={encodedTitle}&api_key={_apiKey}";

			var response = await _httpClient.GetStringAsync(url);
			var result = JsonSerializer.Deserialize<TmdbSearchResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			return result?.Results.FirstOrDefault();
		}

		public async Task<TmdbCreditsResponse?> GetMovieCreditsAsync(long tmdbId)
		{
			var response = await _httpClient.GetStringAsync($"{_baseUrl}/movie/{tmdbId}/credits?api_key={_apiKey}");
			return JsonSerializer.Deserialize<TmdbCreditsResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		public async Task<TmdbMovieDetailsResponse?> GetMovieDetailsAsync(long tmdbId)
		{
			// Gọi API lấy chi tiết phim
			var url = $"{_baseUrl}/movie/{tmdbId}?api_key={_apiKey}";
			var response = await _httpClient.GetStringAsync(url);

			return JsonSerializer.Deserialize<TmdbMovieDetailsResponse>(response,
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}
	}
}