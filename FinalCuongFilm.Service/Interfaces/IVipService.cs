using FinalCuongFilm.ApplicationCore.Entities;

namespace FinalCuongFilm.Service.Interfaces
{
	public interface IVipService
	{
		// 1. Lấy danh sách gói cước
		Task<IEnumerable<VipPackage>> GetActivePackagesAsync();

		Task<VipPackage?> GetPackageByIdAsync(Guid packageId);

		// 2. Kiểm tra User có đang là VIP không
		Task<bool> HasActiveVipAsync(string userId);
		Task<UserSubscription?> GetCurrentUserSubscriptionAsync(string userId);

		// 3. Xử lý Giao dịch (ZaloPay)
		Task<Transaction> CreateTransactionAsync(string userId, Guid packageId);


		Task CompleteTransactionAsync(Guid transactionId, bool isSuccess);


		Task<IEnumerable<VipPackage>> GetAllPackagesAsync(); 
		Task CreatePackageAsync(VipPackage package);
		Task UpdatePackageAsync(VipPackage package);
		Task DeactivatePackageAsync(Guid packageId); 
	
	}
}