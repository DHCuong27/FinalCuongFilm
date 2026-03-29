using FinalCuongFilm.ApplicationCore.Entities.Identity; // Đường dẫn tới file User của bạn
using System;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class WatchHistory
	{
		public Guid Id { get; set; }

		public string UserId { get; set; } = string.Empty;
		public CuongFilmUser User { get; set; }

		public Guid MovieId { get; set; }
		public Movie Movie { get; set; }

		public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
	}
}