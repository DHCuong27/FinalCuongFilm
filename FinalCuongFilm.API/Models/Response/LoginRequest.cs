namespace FinalCuongFilm.API.Models.Response
{
	public class LoginRequest
	{
		public string Email { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class LoginResponse
	{
		public string Token { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string UserName { get; set; } = string.Empty;
		public IList<string> Roles { get; set; } = new List<string>();
		public DateTime ExpiresAt { get; set; }
	}
}