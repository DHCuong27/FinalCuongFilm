using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.Common.DTOs
{
	public class MovieDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? Description { get; set; }
		public int? ReleaseYear { get; set; }
		public long ViewCount { get; set; }
		public int? DurationMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public string? TrailerUrl { get; set; }
		public MovieType Type { get; set; }
		public MovieStatus Status { get; set; }
		public bool IsActive { get; set; }
		public Guid LanguageId { get; set; }
		public Guid CountryId { get; set; }

		// Navigation properties cho hiển thị
		public string? CountryName { get; set; }
		public string? LanguageName { get; set; }

		public string? GenreName { get; set; }

		// Collections cho many-to-many
		public List<Guid> SelectedActorIds { get; set; } = new();
		public List<Guid> SelectedGenreIds { get; set; } = new();
	}

	public class MovieCreateDto
	{
		public string Title { get; set; } = string.Empty;
		public string? Slug { get; set; }
		public string? Description { get; set; }
		public int? ReleaseYear { get; set; }
		public int? DurationMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public string? TrailerUrl { get; set; }
		public MovieType Type { get; set; }
		public MovieStatus Status { get; set; }
		public bool IsActive { get; set; } = true;
		public Guid LanguageId { get; set; }
		public Guid CountryId { get; set; }

		public List<Guid> ActorIds { get; set; } = new();
		public List<Guid> GenreIds { get; set; } = new();
	}

	public class MovieUpdateDto : MovieCreateDto
	{
		public Guid Id { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}