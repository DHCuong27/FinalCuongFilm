using System;
using System.Collections.Generic;
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

		public string PaymentMethod { get; set; } = "VNPAY";
		public Enum.TransactionStatus Status { get; set; } = Enum.TransactionStatus.Pending;

		public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
		
	}
}
