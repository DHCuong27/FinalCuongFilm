using FinalCuongFilm.ApplicationCore.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Favorite
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		
		public Guid MovieId { get; set; }


		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
