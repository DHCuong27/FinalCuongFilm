using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class VipPackage
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public decimal Price { get; set; }
		public int DurationInDays { get; set; }
		public string Description { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;
	}
}
