using System.Collections.Generic;

namespace FinalCuongFilm.Common.DTOs
{
	public class TmdbSearchResponse
	{
		public List<TmdbMovieDto> Results { get; set; } = new List<TmdbMovieDto>();
	}

	public class TmdbMovieDto
	{
		public long Id { get; set; } // TmdbId
		public string Title { get; set; } = string.Empty;
		public string Overview { get; set; } = string.Empty;
		public string PosterPath { get; set; } = string.Empty;
		public string ReleaseDate { get; set; } = string.Empty;
	}

	public class TmdbCreditsResponse
	{
		public List<TmdbCastDto> Cast { get; set; } = new List<TmdbCastDto>();
	}

	public class TmdbCastDto
	{
		public long Id { get; set; } // TmdbId
		public string Name { get; set; } = string.Empty;
		public string ProfilePath { get; set; } = string.Empty;
		public int Gender { get; set; }
	}

	public class TmdbMovieDetailsResponse
	{
		public long Id { get; set; }
		public string Title { get; set; }
		public string Overview { get; set; }
		public string PosterPath { get; set; }

		public string ReleaseDate { get; set; } // <-- Add this property to fix CS0117
		public int Runtime { get; set; }

		public List<TmdbGenreDto>? Genres { get; set; }
		public List<TmdbCountryDto>? ProductionCountries { get; set; }
	}

	public class TmdbGenreDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class TmdbCountryDto
	{
		public string Iso31661 { get; set; } 
		public string Name { get; set; }       // Tên quốc gia
	}
}