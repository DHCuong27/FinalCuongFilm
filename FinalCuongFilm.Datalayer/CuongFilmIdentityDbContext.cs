using FinalCuongFilm.ApplicationCore.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinalCuongFilm.DataLayer // Adjust this namespace to match your project structure
{
	public class CuongFilmIdentityDbContext : IdentityDbContext<CuongFilmUser, CuongFilmRole, string>
	{
		public CuongFilmIdentityDbContext(DbContextOptions<CuongFilmIdentityDbContext> options)
			: base(options)
		{
		}

		protected override void OnModelCreating(ModelBuilder builder)
		{

			base.OnModelCreating(builder);

	
			builder.Entity<CuongFilmUser>().ToTable("Users");
			builder.Entity<CuongFilmRole>().ToTable("Roles");
			builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
			builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
			builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
			builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
			builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
		}
	}
}