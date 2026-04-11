using System;
using System.Collections.Generic;
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

	}
}
