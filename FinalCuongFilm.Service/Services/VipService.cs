using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.ApplicationCore.Entities.Identity;
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

	//  implementation 
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
			if (string.IsNullOrWhiteSpace(userId)) throw new InvalidOperationException("A signed-in user is required to create a VIP transaction.");

			var package = await _context.VipPackages.FindAsync(packageId);
			if (package == null || !package.IsActive) throw new Exception("VIP package is invalid.");

			await EnsurePaymentUserExistsAsync(userId);

			var transaction = new Transaction
			{
				Id = Guid.NewGuid(),
				UserId = userId,
				PackageId = packageId,
				Amount = package.Price,
				OrderInfo = $"Upgrade {package.Name}", 
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
				await EnsurePaymentUserExistsAsync(transaction.UserId);

				var package = await _context.VipPackages.FindAsync(transaction.PackageId);
				if (package == null) return;

				// 1. Update Transaction status
				transaction.Status = TransactionStatus.Success;

				// 2. Check if the user already has an ACTIVE subscription
				var existingSub = await _context.UserSubscriptions
					.Where(s => s.UserId == transaction.UserId && s.IsActive && s.EndDate > DateTime.UtcNow)
					.OrderByDescending(s => s.EndDate)
					.FirstOrDefaultAsync();

				if (existingSub != null)
				{	
					existingSub.EndDate = existingSub.EndDate.AddDays(package.DurationInDays);	
					existingSub.PackageId = package.Id;

					_context.UserSubscriptions.Update(existingSub);
				}
				else
				{
					//  Create a brand new subscription (Tạo mới)
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

		private async Task EnsurePaymentUserExistsAsync(string userId)
		{
			var exists = await _context.Set<CuongFilmUser>().AnyAsync(user => user.Id == userId);
			if (exists) return;

			_context.Set<CuongFilmUser>().Add(new CuongFilmUser
			{
				Id = userId,
				UserName = $"payment-user-{userId}",
				NormalizedUserName = $"PAYMENT-USER-{userId}".ToUpperInvariant(),
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false,
				LockoutEnabled = false,
				AccessFailedCount = 0,
				CreatedAt = DateTime.UtcNow,
				SecurityStamp = Guid.NewGuid().ToString("N"),
				ConcurrencyStamp = Guid.NewGuid().ToString("N")
			});

			await _context.SaveChangesAsync();
		}

		// Get All Packages (Admin)
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


