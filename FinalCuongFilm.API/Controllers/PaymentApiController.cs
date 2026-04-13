using FinalCuongFilm.ApplicationCore.Entities;
using FinalCuongFilm.DataLayer;
using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace FinalCuongFilm.MVC.Controllers
{
	[Route("api/payment")]
	[ApiController]
	public class PaymentApiController : ControllerBase
	{
		private readonly IVipService _vipService;
		private readonly IConfiguration _config;

		public PaymentApiController(IVipService vipService, IConfiguration config)
		{
			_vipService = vipService;
			_config = config;
		}


		[HttpPost("zalopay-callback")]
		public async Task<IActionResult> ZaloPayCallback([FromBody] dynamic cbdata)
		{
			var result = new Dictionary<string, object>();
			try
			{
				var dataStr = Convert.ToString(cbdata["data"]);
				var reqMac = Convert.ToString(cbdata["mac"]);

				// 1. Validate Signature (Bảo mật P0) dùng Key2
				var mac = HmacSHA256(dataStr, _config["ZaloPay:Key2"]);
				if (!reqMac.Equals(mac))
				{
					result["return_code"] = -1;
					result["return_message"] = "mac not equal";
					return Ok(result); // Trả về Ok nhưng code -1 để ZaloPay biết lỗi
				}

				// 2. Parse data
				var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
				var appTransId = Convert.ToString(dataJson["app_trans_id"]);
				var transactionIdStr = appTransId.Split('_')[1];
				var transactionId = Guid.Parse(transactionIdStr);

				// 3. Cập nhật VIP an toàn (Gộp xử lý P1 Idempotency vào service)
				await _vipService.CompleteTransactionAsync(transactionId, true);

				result["return_code"] = 1;
				result["return_message"] = "success";
			}
			catch (Exception ex)
			{
				result["return_code"] = 0; // ZaloPay sẽ gửi lại sau
				result["return_message"] = ex.Message;
			}
			return Ok(result);
		}
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