using FinalCuongFilm.Common.DTOs;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FinalCuongFilm.Service.Services
{
	public class TmdbService : ITmdbService
	{
		private readonly HttpClient _httpClient;
		private readonly string _apiKey;
		private readonly string _baseUrl = "https://api.themoviedb.org/3";

		public TmdbService(HttpClient httpClient, IConfiguration config)
		{
			_httpClient = httpClient;
			_apiKey = config["TmdbSettings:ApiKey"];
		}

		//  MOVIE METHODS 
		public async Task<TmdbMovieDto?> SearchMovieAsync(string title)
		{
			var encodedTitle = Uri.EscapeDataString(title);
			var url = $"{_baseUrl}/search/movie?query={encodedTitle}&api_key={_apiKey}";

			var response = await _httpClient.GetStringAsync(url);
			var result = JsonSerializer.Deserialize<TmdbSearchResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			return result?.Results.FirstOrDefault();
		}

		// Get  Movie Credits (Cast & Crew)
		public async Task<TmdbCreditsResponse?> GetMovieCreditsAsync(long tmdbId)
		{
			var response = await _httpClient.GetStringAsync($"{_baseUrl}/movie/{tmdbId}/credits?api_key={_apiKey}");
			return JsonSerializer.Deserialize<TmdbCreditsResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		// Get Movie Details (including genres, production countries, etc.)
		public async Task<TmdbMovieDetailsResponse?> GetMovieDetailsAsync(long tmdbId)
		{
			var url = $"{_baseUrl}/movie/{tmdbId}?api_key={_apiKey}";
			var response = await _httpClient.GetStringAsync(url);
			return JsonSerializer.Deserialize<TmdbMovieDetailsResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}

		//  TV SHOW METHODS (NEW) 
		public async Task<TmdbMovieDto?> SearchTvShowAsync(string title)
		{
			var encodedTitle = Uri.EscapeDataString(title);
			var url = $"{_baseUrl}/search/tv?query={encodedTitle}&api_key={_apiKey}";

			var response = await _httpClient.GetStringAsync(url);

			// Dùng dynamic để map field "name" của TV Show sang "title" của DTO
			using var doc = JsonDocument.Parse(response);
			var results = doc.RootElement.GetProperty("results");

			if (results.GetArrayLength() == 0) return null;

			var firstResult = results[0];

			return new TmdbMovieDto
			{
				Id = firstResult.GetProperty("id").GetInt64(),
				Title = firstResult.GetProperty("name").GetString() ?? "", // TV dùng 'name'
				Overview = firstResult.TryGetProperty("overview", out var ov) ? ov.GetString() : "",
				PosterPath = firstResult.TryGetProperty("poster_path", out var pp) ? pp.GetString() : "",
				ReleaseDate = firstResult.TryGetProperty("first_air_date", out var fad) ? fad.GetString() : "" // TV dùng 'first_air_date'
			};
		}

		public async Task<TmdbMovieDetailsResponse?> GetTvShowDetailsAsync(long tmdbId)
		{
			var url = $"{_baseUrl}/tv/{tmdbId}?api_key={_apiKey}";
			var response = await _httpClient.GetStringAsync(url);

			using var doc = JsonDocument.Parse(response);
			var root = doc.RootElement;

			// Map thủ công JSON TV Show sang TmdbMovieDetailsResponse
			var details = new TmdbMovieDetailsResponse
			{
				Id = root.GetProperty("id").GetInt64(),
				Title = root.GetProperty("name").GetString() ?? "",
				Overview = root.TryGetProperty("overview", out var ov) ? ov.GetString() : "",
				PosterPath = root.TryGetProperty("poster_path", out var pp) ? pp.GetString() : "",
				ReleaseDate = root.TryGetProperty("first_air_date", out var fad) ? fad.GetString() : "",
				Runtime = root.TryGetProperty("episode_run_time", out var ert) && bindingRunTime(ert) > 0 ? bindingRunTime(ert) : 45,
				Genres = JsonSerializer.Deserialize<List<TmdbGenreDto>>(root.GetProperty("genres").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
				ProductionCountries = JsonSerializer.Deserialize<List<TmdbCountryDto>>(root.GetProperty("production_countries").GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
			};

			return details;
		}

		// Helper để lấy thời lượng tập phim (vì nó trả về mảng int[])
		private int bindingRunTime(JsonElement element)
		{
			if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
				return element[0].GetInt32();
			return 0;
		}

		public async Task<TmdbCreditsResponse?> GetTvCreditsAsync(long tmdbId)
		{
			var response = await _httpClient.GetStringAsync($"{_baseUrl}/tv/{tmdbId}/credits?api_key={_apiKey}");
			return JsonSerializer.Deserialize<TmdbCreditsResponse>(response, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		}
	}
}