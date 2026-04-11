using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinalCuongFilm.MVC.Controllers
{
	[Route("api/payment")]
	[ApiController]
	public class PaymentApiController : ControllerBase
	{
		private readonly IVipService _vipService;

		public PaymentApiController(IVipService vipService)
		{
			_vipService = vipService;
		}

		// API này VNPay sẽ gọi ngầm vào (Webhook / IPN)
		[HttpGet("vnpay-ipn")]
		public async Task<IActionResult> VnpayIpn()
		{
			try
			{
				// 1. Lấy dữ liệu VNPay gửi về
				var vnp_TxnRef = Request.Query["vnp_TxnRef"].ToString();
				var vnp_ResponseCode = Request.Query["vnp_ResponseCode"].ToString();
				var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();

				// TODO: Khi chạy thật, bạn sẽ dùng thư viện VNPay để check vnp_SecureHash tại đây
				// bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, "SECRET_KEY");
				// if (!isValidSignature) return Ok(new { RspCode = "97", Message = "Invalid Signature" });

				if (string.IsNullOrEmpty(vnp_TxnRef) || !Guid.TryParse(vnp_TxnRef, out Guid transactionId))
				{
					return Ok(new { RspCode = "01", Message = "Order not found" });
				}

				// 2. Gọi Service xử lý hoàn tất giao dịch
				bool isProcessed = await _vipService.CompleteTransactionAsync(transactionId, vnp_ResponseCode);

				if (!isProcessed)
				{
					return Ok(new { RspCode = "02", Message = "Order already confirmed or Invalid" });
				}

				// 3. Trả về mã 00 cho VNPay biết là đã nhận tin thành công
				return Ok(new { RspCode = "00", Message = "Confirm Success" });
			}
			catch (Exception ex)
			{
				// Lưu log lỗi nếu cần
				return Ok(new { RspCode = "99", Message = "Unknown error: " + ex.Message });
			}
		}
	}
}