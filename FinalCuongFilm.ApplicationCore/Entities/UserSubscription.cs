using FinalCuongFilm.ApplicationCore.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class UserSubscription
	{
		public Guid Id { get; set; }
		public string UserId { get; set; } = string.Empty;
		public Guid PackageId { get; set; }
		public DateTime StartDate { get; set; } = DateTime.UtcNow;
		public DateTime EndDate { get; set; }

		public bool IsActive { get; set; } = true;

		[ForeignKey("UserId")]
		public virtual CuongFilmUser User { get; set; }

	}
}
