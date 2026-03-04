using System.ComponentModel.DataAnnotations;

namespace FinalCuongFilm.API.Models.Response
{
	public class RegisterRequest
	{
		[Required]
		[EmailAddress]
		public string Email { get; set; } = string.Empty;

		[Required]
		[MinLength(6)]
		public string Password { get; set; } = string.Empty;

		[Required]
		public string UserName { get; set; } = string.Empty;

		public string? FullName { get; set; }
	}
}