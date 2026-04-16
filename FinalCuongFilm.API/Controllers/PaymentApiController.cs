using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace FinalCuongFilm.API.Controllers
{
	// DTO to ensure accurate data binding from ZaloPay's webhook
	public class ZaloPayCallbackDto
	{
		public string data { get; set; } = string.Empty;
		public string mac { get; set; } = string.Empty;
		public int type { get; set; }
	}

	[Route("api/payment")]
	[ApiController]
	public class PaymentApiController : ControllerBase
	{
		private readonly IVipService _vipService;
		private readonly IConfiguration _config;
		private readonly ILogger<PaymentApiController> _logger;

		public PaymentApiController(
			IVipService vipService,
			IConfiguration config,
			ILogger<PaymentApiController> logger)
		{
			_vipService = vipService;
			_config = config;
			_logger = logger;
		}

		[HttpPost("zalopay-callback")]
		public async Task<IActionResult> ZaloPayCallback([FromBody] ZaloPayCallbackDto cbdata)
		{
			var result = new Dictionary<string, object>();

			try
			{
				if (cbdata == null || string.IsNullOrWhiteSpace(cbdata.data) || string.IsNullOrWhiteSpace(cbdata.mac))
				{
					result["return_code"] = -1;
					result["return_message"] = "invalid payload";
					return Ok(result);
				}

				string key2 = _config["ZaloPay:Key2"] ?? string.Empty;
				string calcMac = ComputeHmacSha256(cbdata.data, key2);

				if (!cbdata.mac.Equals(calcMac, StringComparison.OrdinalIgnoreCase))
				{
					result["return_code"] = -1;
					result["return_message"] = "mac not equal";
					return Ok(result);
				}

				var dataJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(cbdata.data);
				string appTransId = Convert.ToString(dataJson?["app_trans_id"]) ?? string.Empty;

				var parts = appTransId.Split('_', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
				{
					result["return_code"] = -1;
					result["return_message"] = "invalid app_trans_id format";
					return Ok(result);
				}

				string txnGuidStr = parts[1];

				if (!Guid.TryParseExact(txnGuidStr, "N", out Guid transactionId))
				{
					if (!Guid.TryParse(txnGuidStr, out transactionId))
					{
						result["return_code"] = -1;
						result["return_message"] = "cannot parse transaction id";
						return Ok(result);
					}
				}

				await _vipService.CompleteTransactionAsync(transactionId, true);

				result["return_code"] = 1;
				result["return_message"] = "success";
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ZaloPay callback failed");
				result["return_code"] = 0;
				result["return_message"] = "internal error";
				return Ok(result);
			}
		}

		private static string ComputeHmacSha256(string data, string key)
		{
			byte[] keyBytes = Encoding.UTF8.GetBytes(key);
			byte[] dataBytes = Encoding.UTF8.GetBytes(data);

			using var hmac = new HMACSHA256(keyBytes);
			byte[] hash = hmac.ComputeHash(dataBytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}
	}
}