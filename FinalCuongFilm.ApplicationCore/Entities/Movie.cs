using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Movie
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Title { get; set; } = string.Empty;
		public string Slug { get; set; } = string.Empty;
		public string? Description { get; set; }
		public int? ReleaseYear { get; set; }

		public int? DurationMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public string? TrailerUrl { get; set; }
		
		public long ViewCount { get; set; }

		public MovieType Type { get; set; }
		public MovieStatus Status { get; set; }

		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime? UpdatedAt { get; set; }

		public Guid LanguageId { get; set; }
		public Language? Language { get; set; } 

		public Guid CountryId	{ get; set; }
		public Country? Country { get; set; } 

		public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
		public ICollection<Movie_Actor> Movie_Actors { get; set; } = new List<Movie_Actor>();
		public ICollection<Movie_Genre> Movie_Genres { get; set; } = new List<Movie_Genre>();
		public ICollection<Movie_Tag> Movie_Tags { get; set; } = new List<Movie_Tag>();
		public ICollection<Review> Reviews { get; set; } = new List<Review>();
		public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();

	}
}
