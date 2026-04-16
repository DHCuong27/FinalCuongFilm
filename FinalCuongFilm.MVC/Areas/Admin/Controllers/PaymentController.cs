using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace FinalCuongFilm.MVC.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentController : ControllerBase
	{
		private readonly IVipService _vipService;
		private readonly IConfiguration _config;

		public PaymentController(IVipService vipService, IConfiguration config)
		{
			_vipService = vipService;
			_config = config;
		}

		// ĐỊA CHỈ NÀY LÀ: /api/payment/zalopay-callback (KHỚP VỚI CẤU HÌNH NGROK CỦA BẠN)
		[HttpPost("zalopay-callback")]
		public async Task<IActionResult> ZaloPayCallback([FromBody] dynamic cbdata)
		{
			var result = new Dictionary<string, object>();

			try
			{
				// 1. Kiểm tra chữ ký (MAC) để chống giả mạo
				string dataStr = Convert.ToString(cbdata["data"]);
				string reqMac = Convert.ToString(cbdata["mac"]);
				string key2 = _config["ZaloPay:Key2"]; // Key2 dùng để verify callback

				string mac = HmacSHA256(dataStr, key2);

				// Nếu chữ ký không khớp -> Đây là request giả mạo (hacker)
				if (!reqMac.Equals(mac))
				{
					result["return_code"] = -1;
					result["return_message"] = "mac not equal";
					return Ok(result); // Trả về Ok() theo chuẩn ZaloPay, nhưng báo lỗi bên trong
				}

				// 2. Chữ ký hợp lệ -> Lấy thông tin giao dịch từ ZaloPay
				var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
				string appTransId = Convert.ToString(dataJson["app_trans_id"]);

				// App_trans_id của bạn có dạng: yyMMdd_Guid
				// Cần tách chuỗi để lấy ra Guid gốc của Transaction trong database
				string[] transParts = appTransId.Split('_');
				if (transParts.Length < 2)
				{
					result["return_code"] = -1;
					result["return_message"] = "Invalid app_trans_id format";
					return Ok(result);
				}

				string txnGuidStr = transParts[1];

				// 3. XỬ LÝ GIAO DỊCH (Gọi hàm Service của bạn)
				if (Guid.TryParse(txnGuidStr, out Guid transactionId))
				{
					// Truyền isSuccess = true vì ZaloPay gọi callback khi giao dịch đã thành công
					await _vipService.CompleteTransactionAsync(transactionId, true);

					// Trả về chuẩn JSON để ZaloPay biết bạn đã nhận thành công, ngừng gọi lại
					result["return_code"] = 1;
					result["return_message"] = "success";
				}
				else
				{
					result["return_code"] = 0;
					result["return_message"] = "Cannot parse Transaction Guid";
				}
			}
			catch (Exception ex)
			{
				// Có lỗi hệ thống
				result["return_code"] = 0;
				result["return_message"] = ex.Message;
			}

			// BẮT BUỘC PHẢI TRẢ VỀ JSON OK THEO CHUẨN ZALOPAY
			return Ok(result);
		}

		// Hàm helper sinh chữ ký
		private string HmacSHA256(string inputData, string key)
		{
			byte[] keyByte = Encoding.UTF8.GetBytes(key);
			byte[] messageBytes = Encoding.UTF8.GetBytes(inputData);
			using (var hmacsha256 = new HMACSHA256(keyByte))
			{
				byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
				return BitConverter.ToString(hashmessage).Replace("-", "").ToLower();
			}
		}
	}
}