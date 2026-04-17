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

		// SỬA LỖI 2: Bổ sung implementation cho hàm này
		public async Task<VipPackage?> GetPackageByIdAsync(Guid packageId)
		{
			return await _context.VipPackages.FindAsync(packageId);
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
				OrderInfo = $"Upgrade {package.Name}", // Tiếng Việt không dấu cho ZaloPay
				Status = TransactionStatus.Pending,
				TransactionDate = DateTime.UtcNow
			};

			_context.Transactions.Add(transaction);
			await _context.SaveChangesAsync();
			return transaction;
		}

		public async Task CompleteTransactionAsync(Guid transactionId, bool isSuccess)
		{
			var transaction = await _context.Transactions.FindAsync(transactionId);
			if (transaction == null || transaction.Status != TransactionStatus.Pending) return;

			if (isSuccess)
			{
				var package = await _context.VipPackages.FindAsync(transaction.PackageId);
				if (package == null) return;

				// 1. Update Transaction status
				transaction.Status = TransactionStatus.Success;

				// 2. CRITICAL LOGIC FIX: Check if the user already has an ACTIVE subscription
				var existingSub = await _context.UserSubscriptions
					.Where(s => s.UserId == transaction.UserId && s.IsActive && s.EndDate > DateTime.UtcNow)
					.OrderByDescending(s => s.EndDate)
					.FirstOrDefaultAsync();

				if (existingSub != null)
				{
					// SCENARIO A: Extend the existing subscription (Cộng dồn ngày)
					existingSub.EndDate = existingSub.EndDate.AddDays(package.DurationInDays);

					// Optional: Update PackageId in case they bought a higher tier
					existingSub.PackageId = package.Id;

					_context.UserSubscriptions.Update(existingSub);
				}
				else
				{
					// SCENARIO B: Create a brand new subscription (Tạo mới)
					var newSub = new UserSubscription
					{
						Id = Guid.NewGuid(),
						UserId = transaction.UserId,
						PackageId = package.Id,
						StartDate = DateTime.UtcNow,
						EndDate = DateTime.UtcNow.AddDays(package.DurationInDays),
						IsActive = true
					};
					_context.UserSubscriptions.Add(newSub);
				}
			}
			else
			{
				transaction.Status = TransactionStatus.Failed;
			}

			await _context.SaveChangesAsync();
		}

		// Lấy TẤT CẢ gói (cả Active lẫn Inactive) để Admin quản lý
		public async Task<IEnumerable<VipPackage>> GetAllPackagesAsync()
		{
			return await _context.VipPackages
				.OrderByDescending(p => p.IsActive)
				.ThenBy(p => p.Price)
				.ToListAsync();
		}

		public async Task CreatePackageAsync(VipPackage package)
		{
			package.Id = Guid.NewGuid();
			_context.VipPackages.Add(package);
			await _context.SaveChangesAsync();
		}

		public async Task UpdatePackageAsync(VipPackage package)
		{
			_context.VipPackages.Update(package);
			await _context.SaveChangesAsync();
		}

		// Thay vì Delete hẳn, ta chỉ ẩn nó đi (Soft Delete)
		public async Task DeactivatePackageAsync(Guid packageId)
		{
			var package = await _context.VipPackages.FindAsync(packageId);
			if (package != null)
			{
				package.IsActive = false;
				package.IsPopular = false; 
				await _context.SaveChangesAsync();
			}
		}
	}
}