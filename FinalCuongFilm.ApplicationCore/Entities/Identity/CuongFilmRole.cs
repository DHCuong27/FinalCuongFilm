
using Microsoft.AspNetCore.Identity;

namespace FinalCuongFilm.ApplicationCore.Entities.Identity
{
	public class CuongFilmRole : IdentityRole
	{
		public string? Description { get; set; }
	}
}
