using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;
using PiedraAzul.Seeders;

namespace PiedraAzul.Extensions
{
    public static class SeedExtensions
    {
        public static async Task SeedAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();


            string[] roles = { "Admin", "Doctor", "Patient" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await DbSeeder.SeedAsync(context, userManager, roleManager);
        }
    }
}