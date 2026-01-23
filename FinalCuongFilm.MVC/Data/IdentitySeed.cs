using FinalCuongFilm.ApplicationCore.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace FinalCuongFilm.MVC.Data
{
	public static class IdentitySeed
	{
		public static async Task SeedAsync(IServiceProvider services)
		{
			using var scope = services.CreateScope();

			var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<CuongFilmRole>>();
			var userManager = scope.ServiceProvider.GetRequiredService<UserManager<CuongFilmUser>>();

			// 1. Roles
			string[] roles = { "Admin", "User" };

			foreach (var role in roles)
			{
				if (!await roleManager.RoleExistsAsync(role))
				{
					await roleManager.CreateAsync(new CuongFilmRole { Name = role });
				}
			}

			// 2. Admin user
			var adminEmail = "admin@cuongfilm.com";
			var adminUser = await userManager.FindByEmailAsync(adminEmail);

			if (adminUser == null)
			{
				adminUser = new CuongFilmUser
				{
					UserName = adminEmail,
					Email = adminEmail,
					FullName = "System Admin",
					EmailConfirmed = true
				};

				var result = await userManager.CreateAsync(adminUser, "Admin@123");

				if (result.Succeeded)
				{
					await userManager.AddToRoleAsync(adminUser, "Admin");
				}
			}
		}
	}
}
