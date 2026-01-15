using Microsoft.AspNetCore.Identity;

namespace FinalCuongFilm.ApplicationCore.Entities.Identity
{
	public class CuongFilmUser : IdentityUser
	{
		public string? FullName { get; set; }
		public string? AvatarUrl { get; set; }
		public string? Gender { get; set; }
	}
}
