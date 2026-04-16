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
				OrderInfo = $"Nang cap {package.Name}", // Tiếng Việt không dấu cho VNPay/ZaloPay
				Status = TransactionStatus.Pending,
				TransactionDate = DateTime.UtcNow
			};

			_context.Transactions.Add(transaction);
			await _context.SaveChangesAsync();
			return transaction;
		}

		//public async Task CompleteTransactionAsync(Guid transactionId, bool isSuccess)
		//{
		//	var transaction = await _context.Transactions.FindAsync(transactionId);
		//	if (transaction == null || transaction.Status != TransactionStatus.Pending) return; // Idempotency check

		//	transaction.Status = isSuccess ? TransactionStatus.Success : TransactionStatus.Failed;

		//	if (isSuccess)
		//	{
		//		var package = await _context.VipPackages.FindAsync(transaction.PackageId);

		//		// FIX LỖI P0: Chỉ lấy sub ĐANG ACTIVE (hoặc null nếu chưa có/đã hết hạn)
		//		var activeSub = await _context.UserSubscriptions
		//			.FirstOrDefaultAsync(s => s.UserId == transaction.UserId && s.IsActive && s.EndDate > DateTime.UtcNow);

		//		if (activeSub != null)
		//		{
		//			// Gia hạn và Cập nhật PackageId mới nếu user đổi gói (FIX P1)
		//			activeSub.EndDate = activeSub.EndDate.AddDays(package.DurationInDays);
		//			activeSub.PackageId = package.Id;
		//		}
		//		else
		//		{
		//			// Tạo mới
		//			var newSub = new UserSubscription
		//			{
		//				UserId = transaction.UserId,
		//				PackageId = package.Id,
		//				StartDate = DateTime.UtcNow,
		//				EndDate = DateTime.UtcNow.AddDays(package.DurationInDays),
		//				IsActive = true
		//			};
		//			_context.UserSubscriptions.Add(newSub);
		//		}
		//	}

		//	await _context.SaveChangesAsync();
		//}

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
		public async Task CompleteTransactionAsync(Guid transactionId, bool isSuccess)
		{
			// 1. Lấy giao dịch
			var transaction = await _context.Transactions.FindAsync(transactionId);

			// Idempotency check: Tránh xử lý đúp nếu ZaloPay gọi webhook nhiều lần
			if (transaction == null || transaction.Status != FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Pending)
				return;

			if (isSuccess)
			{
				// 2. PHẢI TÌM ĐƯỢC GÓI VIP TRƯỚC KHI CẬP NHẬT TRẠNG THÁI GIAO DỊCH
				var package = await _context.VipPackages.FindAsync(transaction.PackageId);

				if (package == null)
				{
					// LỖI NGHIÊM TRỌNG: Đã nhận tiền nhưng gói VIP không tồn tại.
					// Phải ghi nhận lại để Admin check tay, KHÔNG ĐƯỢC để Success.
					transaction.Status = FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Failed;
					transaction.OrderInfo += " [LỖI HỆ THỐNG: KHÔNG TÌM THẤY GÓI VIP ĐỂ CẤP CHO USER]";
					await _context.SaveChangesAsync();
					return;
				}

				// 3. Xử lý cấp VIP an toàn
				var activeSub = await _context.UserSubscriptions
					.FirstOrDefaultAsync(s => s.UserId == transaction.UserId && s.IsActive && s.EndDate > DateTime.UtcNow);

				if (activeSub != null)
				{
					// Cập nhật gói mới và cộng dồn ngày
					activeSub.PackageId = package.Id;
					activeSub.EndDate = activeSub.EndDate.AddDays(package.DurationInDays);
				}
				else
				{
					// Tạo mới nếu chưa có hoặc gói cũ đã hết hạn
					_context.UserSubscriptions.Add(new UserSubscription
					{
						Id = Guid.NewGuid(),
						UserId = transaction.UserId,
						PackageId = package.Id,
						StartDate = DateTime.UtcNow,
						EndDate = DateTime.UtcNow.AddDays(package.DurationInDays),
						IsActive = true
					});
				}

				// 4. MỌI THỨ ĐÃ AN TOÀN -> ĐÁNH DẤU SUCCESS
				transaction.Status = FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Success;
			}
			else
			{
				transaction.Status = FinalCuongFilm.ApplicationCore.Entities.Enum.TransactionStatus.Failed;
			}

			// Lưu lại toàn bộ thay đổi (Transaction và Subscription) trong 1 lần commit
			await _context.SaveChangesAsync();
		}
	}
}