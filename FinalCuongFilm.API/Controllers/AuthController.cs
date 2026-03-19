using FinalCuongFilm.API.Models.Response;
using FinalCuongFilm.ApplicationCore.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinalCuongFilm.API.Controllers
{
	/// <summary>
	/// Authentication endpoints (Login / Register)
	/// </summary>
	[Route("api/[controller]")]
	[ApiController]
	[Produces("application/json")]
	public class AuthController : ControllerBase
	{
		private readonly UserManager<CuongFilmUser> _userManager;
		private readonly SignInManager<CuongFilmUser> _signInManager;
		private readonly IConfiguration _config;
		private readonly ILogger<AuthController> _logger;

		public AuthController(
			UserManager<CuongFilmUser> userManager,
			SignInManager<CuongFilmUser> signInManager,
			IConfiguration config,
			ILogger<AuthController> logger)
		{
			_userManager = userManager;
			_signInManager = signInManager;
			_config = config;
			_logger = logger;
		}

		/// <summary>
		/// Login - trả về JWT token
		/// </summary>
		[HttpPost("login")]
		[ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
		public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<LoginResponse>.FailureResult("Dữ liệu không hợp lệ"));

			var user = await _userManager.FindByEmailAsync(request.Email);
			if (user == null)
				return Unauthorized(ApiResponse<LoginResponse>.FailureResult("Email hoặc mật khẩu không đúng", statusCode: 401));

			var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
			if (!result.Succeeded)
				return Unauthorized(ApiResponse<LoginResponse>.FailureResult("Email hoặc mật khẩu không đúng", statusCode: 401));

			var roles = await _userManager.GetRolesAsync(user);
			var token = GenerateJwtToken(user, roles);
			var expireMinutes = int.Parse(_config["Jwt:ExpireMinutes"] ?? "60");

			var response = new LoginResponse
			{
				Token = token,
				Email = user.Email ?? "",
				UserName = user.UserName ?? "",
				Roles = roles,
				ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
			};

			_logger.LogInformation("User {Email} logged in successfully via API", user.Email);
			return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Đăng nhập thành công"));
		}

		/// <summary>
		/// Register - tạo tài khoản mới (role: User)
		/// </summary>
		[HttpPost("register")]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
		{
			if (!ModelState.IsValid)
				return BadRequest(ApiResponse<object>.FailureResult("Dữ liệu không hợp lệ"));

			var existingUser = await _userManager.FindByEmailAsync(request.Email);
			if (existingUser != null)
				return BadRequest(ApiResponse<object>.FailureResult("Email đã được sử dụng"));

			var user = new CuongFilmUser
			{
				Email = request.Email,
				UserName = request.UserName,
				FullName = request.FullName
			};

			var result = await _userManager.CreateAsync(user, request.Password);
			if (!result.Succeeded)
			{
				var errors = result.Errors.Select(e => e.Description).ToList();
				return BadRequest(ApiResponse<object>.FailureResult("Đăng ký thất bại", errors));
			}

			await _userManager.AddToRoleAsync(user, "User");

			_logger.LogInformation("New user registered: {Email}", user.Email);
			return StatusCode(201, ApiResponse<object>.SuccessResult(
				new { userId = user.Id, email = user.Email },
				"Đăng ký thành công",
				statusCode: 201
			));
		}

		// ─── Private ────────────────────────────────────────────────────────────
		private string GenerateJwtToken(CuongFilmUser user, IList<string> roles)
		{
			var jwtSection = _config.GetSection("Jwt");
			var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);
			var expireMinutes = int.Parse(jwtSection["ExpireMinutes"] ?? "60");

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, user.Id),
				new Claim(ClaimTypes.Email,          user.Email ?? ""),
				new Claim(ClaimTypes.Name,           user.UserName ?? ""),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
			};

			foreach (var role in roles)
				claims.Add(new Claim(ClaimTypes.Role, role));

			var credentials = new SigningCredentials(
				new SymmetricSecurityKey(key),
				SecurityAlgorithms.HmacSha256
			);

			var token = new JwtSecurityToken(
				issuer: jwtSection["Issuer"],
				audience: jwtSection["Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddMinutes(expireMinutes),
				signingCredentials: credentials
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}