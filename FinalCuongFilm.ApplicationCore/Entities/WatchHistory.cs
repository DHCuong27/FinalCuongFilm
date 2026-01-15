using FinalCuongFilm.ApplicationCore.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class WatchHistory
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		public Guid MovieId { get; set; }

		public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

		public Guid? EpisodeId { get; set; }
	}
}
