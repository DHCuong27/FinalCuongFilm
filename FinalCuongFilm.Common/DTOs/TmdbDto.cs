using System.Collections.Generic;
using System.Text.Json.Serialization; // BẮT BUỘC PHẢI CÓ USING NÀY

namespace FinalCuongFilm.Common.DTOs
{
	public class TmdbSearchResponse
	{
		[JsonPropertyName("results")]
		public List<TmdbMovieDto> Results { get; set; } = new List<TmdbMovieDto>();
	}

	public class TmdbMovieDto
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; } = string.Empty;

		[JsonPropertyName("overview")]
		public string Overview { get; set; } = string.Empty;

		[JsonPropertyName("poster_path")]
		public string PosterPath { get; set; } = string.Empty;

		[JsonPropertyName("release_date")]
		public string ReleaseDate { get; set; } = string.Empty;
	}

	public class TmdbCreditsResponse
	{
		[JsonPropertyName("cast")]
		public List<TmdbCastDto> Cast { get; set; } = new List<TmdbCastDto>();
	}

	public class TmdbCastDto
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; } = string.Empty;

		[JsonPropertyName("profile_path")]
		public string ProfilePath { get; set; } = string.Empty;

		[JsonPropertyName("gender")]
		public int Gender { get; set; }
	}

	public class TmdbMovieDetailsResponse
	{
		[JsonPropertyName("id")]
		public long Id { get; set; }

		[JsonPropertyName("title")]
		public string Title { get; set; }

		[JsonPropertyName("overview")]
		public string Overview { get; set; }

		[JsonPropertyName("poster_path")]
		public string PosterPath { get; set; }

		// Map ngày phát hành của phim lẻ
		[JsonPropertyName("release_date")]
		public string ReleaseDate { get; set; }

		[JsonPropertyName("runtime")]
		public int Runtime { get; set; }

		[JsonPropertyName("genres")]
		public List<TmdbGenreDto>? Genres { get; set; }

		[JsonPropertyName("production_countries")]
		public List<TmdbCountryDto>? ProductionCountries { get; set; }
	}

	public class TmdbGenreDto
	{
		[JsonPropertyName("id")]
		public int Id { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }
	}

	public class TmdbCountryDto
	{
		[JsonPropertyName("iso_3166_1")] 
		public string Iso31661 { get; set; }

		[JsonPropertyName("name")]
		public string Name { get; set; }
	}
}