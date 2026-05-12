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

		
		[HttpPost("zalopay-callback")]
		public async Task<IActionResult> ZaloPayCallback([FromBody] dynamic cbdata)
		{
			var result = new Dictionary<string, object>();

			try
			{
				string dataStr = Convert.ToString(cbdata["data"]);
				string reqMac = Convert.ToString(cbdata["mac"]);
				string key2 = _config["ZaloPay:Key2"]; // Key2 verify callback

				string mac = HmacSHA256(dataStr, key2);

		
				if (!reqMac.Equals(mac))
				{
					result["return_code"] = -1;
					result["return_message"] = "mac not equal";
					return Ok(result); 
				}

				// Parse data
				var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(dataStr);
				string appTransId = Convert.ToString(dataJson["app_trans_id"]);
	
				string[] transParts = appTransId.Split('_');
				if (transParts.Length < 2)
				{
					result["return_code"] = -1;
					result["return_message"] = "Invalid app_trans_id format";
					return Ok(result);
				}

				string txnGuidStr = transParts[1];

				
				if (Guid.TryParse(txnGuidStr, out Guid transactionId))
				{
					await _vipService.CompleteTransactionAsync(transactionId, true);

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
				result["return_code"] = 0;
				result["return_message"] = ex.Message;
			}

			// BẮT BUỘC PHẢI TRẢ VỀ JSON OK THEO CHUẨN ZALOPAY
			return Ok(result);
		}

		// Sgnature HMAC SHA256
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