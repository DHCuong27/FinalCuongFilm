using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Enum
	{
		public enum MovieType
		{
			Movie = 1,
			Series = 2,
		}

		public enum MovieStatus
		{
			Upcoming = 1,
			Ongoing = 2,
			Completed = 3,
		}
		public enum FileType
		{
			Video,
			Subtitle,
			Trailer
		}

		public enum VideoQuality
		{
			Q360p,
			Q480p,
			Q720p,
			Q1080p,
			Q2160p // 4K
		}
	}
}
