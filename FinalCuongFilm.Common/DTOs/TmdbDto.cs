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
		public string Poster_Path { get; set; } = string.Empty;
		public string Release_Date { get; set; } = string.Empty;
	}

	public class TmdbCreditsResponse
	{
		public List<TmdbCastDto> Cast { get; set; } = new List<TmdbCastDto>();
	}

	public class TmdbCastDto
	{
		public long Id { get; set; } // TmdbId
		public string Name { get; set; } = string.Empty;
		public string Profile_Path { get; set; } = string.Empty;
		public int Gender { get; set; }
	}

	public class TmdbMovieDetailsResponse
	{
		public long Id { get; set; }
		public string Title { get; set; }
		public string Overview { get; set; }
		public string Poster_Path { get; set; }

		public string Release_Date { get; set; } // <-- Add this property to fix CS0117
		public int Runtime { get; set; }

		public List<TmdbGenreDto>? Genres { get; set; }
		public List<TmdbCountryDto>? Production_Countries { get; set; }
	}

	public class TmdbGenreDto
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class TmdbCountryDto
	{
		public string Iso_3166_1 { get; set; } // Mã quốc gia (VD: US, VN, UK)
		public string Name { get; set; }       // Tên quốc gia
	}
}