using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinalCuongFilm.MVC.Controllers
{
	public class PremiumController : Controller
	{
		private readonly IVipService _vipService;
		private readonly IConfiguration _config;
		private readonly ILogger<PremiumController> _logger;

		public PremiumController(
			IVipService vipService,
			IConfiguration config,
			ILogger<PremiumController> logger)
		{
			_vipService = vipService;
			_config = config;
			_logger = logger;
		}

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var packages = await _vipService.GetActivePackagesAsync();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (!string.IsNullOrEmpty(userId))
			{
				var currentVip = await _vipService.GetCurrentUserSubscriptionAsync(userId);
				ViewBag.CurrentVipEndDate = currentVip?.EndDate;
			}

			return View(packages);
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Checkout(Guid packageId)
		{
			var packages = await _vipService.GetActivePackagesAsync();
			var selectedPackage = packages.FirstOrDefault(p => p.Id == packageId);

			if (selectedPackage == null)
			{
				TempData["Error"] = "VIP package does not exist.";
				return RedirectToAction(nameof(Index));
			}

			return View(selectedPackage);
		}

		[Authorize]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ProcessPayment(Guid packageId)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId)) return Challenge();

			var package = await _vipService.GetPackageByIdAsync(packageId);
			if (package == null)
			{
				TempData["Error"] = "VIP package does not exist.";
				return RedirectToAction("Index");
			}

			var appId = _config["ZaloPay:AppId"] ?? "";
			var key1 = _config["ZaloPay:Key1"] ?? "";
			var endpoint = _config["ZaloPay:Endpoint"] ?? "https://sb-openapi.zalopay.vn/v2/create";
			var callbackUrl = _config["ZaloPay:CallbackUrl"] ?? "";
			var redirectUrl = _config["ZaloPay:RedirectUrl"] ?? $"{Request.Scheme}://{Request.Host}/Premium/PaymentSuccess";

			var appUser = $"u_{userId.Replace("-", "")}".ToLowerInvariant();
			if (appUser.Length > 30) appUser = appUser[..30];

			var amount = (int)Math.Round(package.Price, MidpointRounding.AwayFromZero);
			var transaction = await _vipService.CreateTransactionAsync(userId, packageId);

			var appTransId = $"{DateTime.Now:yyMMdd}_{transaction.Id:N}";
			var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

			// KEY CHANGE: Pass both txnId and appTransId back to the Success page via URL parameters
			var redirectUrlWithParams = $"{redirectUrl}?txnId={transaction.Id}&appTransId={appTransId}";
			var embedData = JsonConvert.SerializeObject(new { redirecturl = redirectUrlWithParams });
			var item = "[]";

			var reqData = new Dictionary<string, string>
			{
				{ "app_id", appId },
				{ "app_trans_id", appTransId },
				{ "app_user", appUser },
				{ "app_time", appTime },
				{ "amount", amount.ToString() },
				{ "item", item },
				{ "embed_data", embedData },
				{ "description", $"CuongFilm VIP {package.Name}" },
				{ "callback_url", callbackUrl }
			};

			var dataToSign = $"{reqData["app_id"]}|{reqData["app_trans_id"]}|{reqData["app_user"]}|{reqData["amount"]}|{reqData["app_time"]}|{reqData["embed_data"]}|{reqData["item"]}";
			reqData["mac"] = ComputeHmacSha256(dataToSign, key1);

			try
			{
				using var http = new HttpClient();
				var response = await http.PostAsync(endpoint, new FormUrlEncodedContent(reqData));
				var raw = await response.Content.ReadAsStringAsync();

				dynamic res = JsonConvert.DeserializeObject(raw)!;
				int returnCode = res?.return_code != null ? (int)res.return_code : -999;
				string orderUrl = res?.order_url != null ? (string)res.order_url : "";

				if (returnCode == 1 && !string.IsNullOrWhiteSpace(orderUrl))
					return Redirect(orderUrl);

				TempData["Error"] = "Failed to create ZaloPay transaction.";
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "ProcessPayment exception");
				TempData["Error"] = "ZaloPay connection error.";
				return RedirectToAction("Index");
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> PaymentSuccess(
			[FromQuery] Guid? txnId,
			[FromQuery] string? appTransId,
			[FromQuery] int? status)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrWhiteSpace(userId)) return Challenge();

			_logger.LogInformation("[PAYMENT SUCCESS] Redirected back. txnId: {TxnId}, appTransId: {AppTransId}, status: {Status}", txnId, appTransId, status);

			if (txnId.HasValue && !string.IsNullOrWhiteSpace(appTransId))
			{
				// Execute active query with retry mechanism (Polling)
				await CheckZaloPayOrderStatusWithRetryAsync(appTransId, txnId.Value);
			}
			else
			{
				_logger.LogWarning("[PAYMENT SUCCESS] Missing parameters. Cannot perform active query.");
			}

			var sub = await _vipService.GetCurrentUserSubscriptionAsync(userId);

			if (sub == null || sub.EndDate <= DateTime.UtcNow)
			{
				_logger.LogWarning("[PAYMENT SUCCESS] Verification finished but VIP is not active yet.");
				ViewBag.IsPending = true;
				return View();
			}

			_logger.LogInformation("[PAYMENT SUCCESS] VIP is ACTIVE!");
			ViewBag.IsPending = false;
			ViewBag.ExpiryDate = sub.EndDate.ToString("dd/MM/yyyy");
			return View();
		}

		private async Task CheckZaloPayOrderStatusWithRetryAsync(string appTransId, Guid txnId)
		{
			var appId = _config["ZaloPay:AppId"] ?? "";
			var key1 = _config["ZaloPay:Key1"] ?? "";
			var queryEndpoint = "https://sb-openapi.zalopay.vn/v2/query";

			var dataToSign = $"{appId}|{appTransId}|{key1}";
			var mac = ComputeHmacSha256(dataToSign, key1);

			var reqData = new Dictionary<string, string>
			{
				{ "app_id", appId },
				{ "app_trans_id", appTransId },
				{ "mac", mac }
			};

			int maxRetries = 3;
			int delayMilliseconds = 2000; // 2 seconds between retries

			for (int i = 1; i <= maxRetries; i++)
			{
				try
				{
					_logger.LogInformation("[ACTIVE QUERY] Attempt {Attempt}/{MaxRetries} for appTransId: {AppTransId}", i, maxRetries, appTransId);

					// Delay before querying to give ZaloPay Sandbox time to process
					await Task.Delay(delayMilliseconds);

					using var http = new HttpClient();
					var response = await http.PostAsync(queryEndpoint, new FormUrlEncodedContent(reqData));
					var raw = await response.Content.ReadAsStringAsync();

					_logger.LogInformation("[ACTIVE QUERY] ZaloPay Response: {Raw}", raw);

					dynamic res = JsonConvert.DeserializeObject(raw)!;
					int returnCode = res?.return_code != null ? (int)res.return_code : -999;

					if (returnCode == 1)
					{
						_logger.LogInformation("[ACTIVE QUERY] Status SUCCESS. Updating database...");
						await _vipService.CompleteTransactionAsync(txnId, true);
						return; // Exit loop, job is done
					}

					if (returnCode == 2)
					{
						_logger.LogInformation("[ACTIVE QUERY] Status FAILED. Transaction was rejected.");
						await _vipService.CompleteTransactionAsync(txnId, false);
						return; // Exit loop, failure confirmed
					}

					_logger.LogWarning("[ACTIVE QUERY] Status PENDING or UNKNOWN (Code: {Code}). Retrying...", returnCode);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[ACTIVE QUERY] Exception on attempt {Attempt}", i);
				}
			}

			_logger.LogError("[ACTIVE QUERY] All retries exhausted. Transaction {TxnId} remains PENDING.", txnId);
		}

		private static string ComputeHmacSha256(string data, string key)
		{
			var keyBytes = Encoding.UTF8.GetBytes(key);
			var dataBytes = Encoding.UTF8.GetBytes(data);
			using var hmac = new HMACSHA256(keyBytes);
			var hash = hmac.ComputeHash(dataBytes);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}
	}
}