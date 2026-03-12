using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

		public Guid? LanguageId { get; set; }
		public Language? Language { get; set; } 

		public Guid? CountryId	{ get; set; }
		public Country? Country { get; set; }

		public long? TmdbId { get; set; }

		public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
		public ICollection<MovieActor> MovieActors { get; set; } = new List<MovieActor>();
		public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
		public ICollection<MovieTag> MovieTags { get; set; } = new List<MovieTag>();
		public ICollection<Review> Reviews { get; set; } = new List<Review>();
		public ICollection<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();
		 public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

		[NotMapped]
		public double AverageRating => Reviews.Any()
	   ? Math.Round(Reviews.Where(r => r.IsApproved).Average(r => r.Rating), 1)
	   : 0;

		[NotMapped]
		public int TotalReviews => Reviews.Count(r => r.IsApproved);

		[NotMapped]
		public int TotalFavorites => Favorites.Count;

	}
}
