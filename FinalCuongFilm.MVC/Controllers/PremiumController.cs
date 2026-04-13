using FinalCuongFilm.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace FinalCuongFilm.MVC.Controllers
{
	public class PremiumController : Controller
	{
		private readonly IVipService _vipService;
		private readonly IConfiguration _config;

		public PremiumController(IVipService vipService, IConfiguration config)
		{
			_vipService = vipService;
			_config = config;
		}

		// GET: /Premium/Index
		public async Task<IActionResult> Index()
		{
			var packages = await _vipService.GetActivePackagesAsync();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId != null)
			{
				var currentVip = await _vipService.GetCurrentUserSubscriptionAsync(userId);
				ViewBag.CurrentVipEndDate = currentVip?.EndDate;
			}

			return View(packages);
		}

		// GET: /Premium/Checkout
		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Checkout(Guid packageId)
		{
			var packages = await _vipService.GetActivePackagesAsync();
			var selectedPackage = packages.FirstOrDefault(p => p.Id == packageId);

			if (selectedPackage == null)
			{
				TempData["Error"] = "This package does not exist.";
				return RedirectToAction("Index");
			}

			return View(selectedPackage);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ProcessPayment(Guid packageId)
		{
			// 1. Lấy userId hiện tại
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return Challenge();

			// 2. Lấy thông tin gói Package (ĐÃ FIX: Gán vào biến package và check null)
			var package = await _vipService.GetPackageByIdAsync(packageId);
			if (package == null) return NotFound("Gói VIP không tồn tại");

			// 3. Tạo Transaction
			var transaction = await _vipService.CreateTransactionAsync(userId, packageId);

			// 4. Tạo TimeStamp
			var appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

			var embedData = new { redirecturl = "https://localhost:7237/Premium/PaymentSuccess" };
			var embedDataString = JsonConvert.SerializeObject(embedData);

			// 5. Build data gửi ZaloPay
			var zalopayRequestData = new Dictionary<string, string>
				{
					{ "app_id", _config["ZaloPay:AppId"] },
					{ "app_trans_id", $"{DateTime.Now:yyMMdd}_{transaction.Id.ToString("N")}" },
					{ "app_time", appTime },
					{ "app_user", userId },
					{ "amount", ((long)package.Price).ToString() },
					{ "description", $"VIP Payment CuongFilm - Package {package.Name}" },
					{ "item", "[]" },
					{ "embed_data", embedDataString },
					{ "callback_url", _config["ZaloPay:CallbackUrl"] }
				};

			// 6. Tạo chữ ký (Mac)
			var dataToMac = $"{zalopayRequestData["app_id"]}|{zalopayRequestData["app_trans_id"]}|{zalopayRequestData["app_user"]}|{zalopayRequestData["amount"]}|{zalopayRequestData["app_time"]}|{zalopayRequestData["embed_data"]}|{zalopayRequestData["item"]}";
			zalopayRequestData["mac"] = HmacSHA256(dataToMac, _config["ZaloPay:Key1"]);

			// 7. Gọi API thật tới ZaloPay
			using (var client = new HttpClient())
			{
				var content = new FormUrlEncodedContent(zalopayRequestData);
				var responseMsg = await client.PostAsync(_config["ZaloPay:Endpoint"], content);
				var responseString = await responseMsg.Content.ReadAsStringAsync();

				var responseData = JsonConvert.DeserializeObject<dynamic>(responseString);

				if (responseData.return_code == 1)
				{
					// Lấy order_url và chuyển hướng người dùng tới cổng thanh toán ZaloPay
					return Redirect(responseData.order_url.ToString());
				}

				return BadRequest($"Chi tiết lỗi từ ZaloPay: {responseString}");
			}
		}
		[HttpGet]
		[Authorize] // Bắt buộc đăng nhập
		public async Task<IActionResult> PaymentSuccess()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId)) return RedirectToAction("Index", "Home");

			// Lấy thông tin VIP mới nhất của user
			var currentSub = await _vipService.GetCurrentUserSubscriptionAsync(userId);

			if (currentSub != null)
			{
				// Lấy tên gói VIP
				var package = await _vipService.GetPackageByIdAsync(currentSub.PackageId);
				ViewBag.PackageName = package?.Name ?? "Gói VIP Premium";
				ViewBag.ExpiryDate = currentSub.EndDate.ToString("dd/MM/yyyy HH:mm");
				ViewBag.IsPending = false;
			}
			else
			{
				// Xử lý độ trễ: Trường hợp web quay về đích nhanh hơn ZaloPay bắn Webhook (chưa kịp update DB)
				ViewBag.IsPending = true;
			}

			ViewBag.Message = "Giao dịch thanh toán qua ZaloPay đã hoàn tất!";
			return View();
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