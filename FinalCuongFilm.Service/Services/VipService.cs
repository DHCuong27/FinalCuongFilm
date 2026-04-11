using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.EntityFrameworkCore;
using static FinalCuongFilm.ApplicationCore.Entities.Enum;

namespace FinalCuongFilm.Service.Services
{
	public class VipService : IVipService
	{
		private readonly CuongFilmDbContext _context;

		public VipService(CuongFilmDbContext context)
		{
			_context = context;
		}

		public async Task<IEnumerable<VipPackage>> GetActivePackagesAsync()
		{
			return await _context.VipPackages
				.Where(p => p.IsActive)
				.OrderBy(p => p.Price) // Sắp xếp từ rẻ đến đắt
				.ToListAsync();
		}

		public async Task<bool> HasActiveVipAsync(string userId)
		{
			return await _context.UserSubscriptions
				.AnyAsync(s => s.UserId == userId && s.EndDate > DateTime.UtcNow && s.IsActive);
		}

		public async Task<UserSubscription?> GetCurrentUserSubscriptionAsync(string userId)
		{
			return await _context.UserSubscriptions
				.FirstOrDefaultAsync(s => s.UserId == userId && s.EndDate > DateTime.UtcNow && s.IsActive);
		}

		public async Task<Transaction> CreateTransactionAsync(string userId, Guid packageId)
		{
			var package = await _context.VipPackages.FindAsync(packageId);
			if (package == null || !package.IsActive) throw new Exception("Gói VIP không hợp lệ.");

			var transaction = new Transaction
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				PackageId = packageId,
				Amount = package.Price,
				OrderInfo = $"Nang cap {package.Name}", // Tiếng Việt không dấu cho VNPay
				Status = TransactionStatus.Pending,
				TransactionDate = DateTime.UtcNow
			};

			_context.Transactions.Add(transaction);
			await _context.SaveChangesAsync();
			return transaction;
		}

		public async Task<bool> CompleteTransactionAsync(Guid transactionId, string vnpayResponseCode)
		{
			var transaction = await _context.Transactions.FindAsync(transactionId);
			if (transaction == null || transaction.Status != TransactionStatus.Pending) return false;

			if (vnpayResponseCode == "00") // 00 là mã thành công của VNPay
			{
				transaction.Status = TransactionStatus.Success;
				var package = await _context.VipPackages.FindAsync(transaction.PackageId);

				// Cộng ngày VIP
				var currentSub = await _context.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == transaction.UserId);
				if (currentSub != null)
				{
					var baseDate = currentSub.EndDate > DateTime.UtcNow ? currentSub.EndDate : DateTime.UtcNow;
					currentSub.EndDate = baseDate.AddDays(package!.DurationInDays);
					currentSub.IsActive = true;
				}
				else
				{
					_context.UserSubscriptions.Add(new UserSubscription
					{
						Id = Guid.NewGuid(),
						UserId = transaction.UserId,
						PackageId = package!.Id,
						StartDate = DateTime.UtcNow,
						EndDate = DateTime.UtcNow.AddDays(package.DurationInDays),
						IsActive = true
					});
				}
			}
			else
			{
				transaction.Status = TransactionStatus.Failed;
			}

			await _context.SaveChangesAsync();
			return true;
		}
	}
}