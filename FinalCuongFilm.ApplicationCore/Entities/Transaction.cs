using FinalCuongFilm.ApplicationCore.Entities.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinalCuongFilm.ApplicationCore.Entities
{
	public class Transaction
	{
		public Guid Id { get; set; }
		public string UserId { get; set; } = string.Empty;
		public Guid PackageId { get; set; }
		public decimal Amount { get; set; }

		public string OrderInfo { get; set; } = string.Empty;

		public string PaymentMethod { get; set; } = "ZALOPAY";
		public Enum.TransactionStatus Status { get; set; } = Enum.TransactionStatus.Pending;

		public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

		[ForeignKey("UserId")]
		public virtual CuongFilmUser User { get; set; }
	}
}
