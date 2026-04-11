using FinalCuongFilm.ApplicationCore.Entities;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IVipService
	{
		// 1. Lấy danh sách gói cước
		Task<IEnumerable<VipPackage>> GetActivePackagesAsync();

		// 2. Kiểm tra User có đang là VIP không
		Task<bool> HasActiveVipAsync(string userId);
		Task<UserSubscription?> GetCurrentUserSubscriptionAsync(string userId);

		// 3. Xử lý Giao dịch (Sẽ dùng cho VNPay)
		Task<Transaction> CreateTransactionAsync(string userId, Guid packageId);
		//Task<Transaction?> GetTransactionByIdAsync(Guid transactionId);
		Task<bool> CompleteTransactionAsync(Guid transactionId, string vnpayResponseCode);
	}
}