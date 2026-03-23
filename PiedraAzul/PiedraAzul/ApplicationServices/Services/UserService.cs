using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Data;
using PiedraAzul.Data.Models;

namespace PiedraAzul.ApplicationServices.Services
{
    public interface IUserService
    {
        Task<(ApplicationUser?, List<string>)> Login(string field, string password);
        Task<ApplicationUser?> Register(ApplicationUser applicationUser, string password, List<string> roles);

        Task<List<string>> GetRolesByUser(ApplicationUser user);
        Task<ApplicationUser?> GetById(string userId);
        Task CreateProfileForRoleAsync(ApplicationUser user, string role);
    }
    public class UserService(IDbContextFactory<AppDbContext> dbContext, 
        UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) : IUserService
    {
        public async Task CreateProfileForRoleAsync(ApplicationUser user, string role)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be null or empty", nameof(role));

            using var context = await dbContext.CreateDbContextAsync();

            var normalizedRole = role.Trim().ToLower();

            switch (normalizedRole)
            {
                case "patient":
                    {
                        var exists = await context.PatientProfiles
                            .AnyAsync(p => p.UserId == user.Id);

                        if (exists) return;

                        var patientProfile = new PatientProfile
                        {
                            UserId = user.Id,
                        };

                        await context.PatientProfiles.AddAsync(patientProfile);
                        break;
                    }

                case "doctor":
                    {
                        var exists = await context.DoctorProfiles
                            .AnyAsync(d => d.UserId == user.Id);

                        if (exists) return;

                        var doctorProfile = new DoctorProfile
                        {
                            UserId = user.Id,
                        };

                        await context.DoctorProfiles.AddAsync(doctorProfile);
                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        $"Role '{role}' does not have a corresponding profile.");
            }

            await context.SaveChangesAsync();
        }

        public async Task<ApplicationUser?> GetById(string userId)
        {
            return await userManager.FindByIdAsync(userId);
        }

        public async Task<List<string>> GetRolesByUser(ApplicationUser user)
        {
            var roles = await userManager.GetRolesAsync(user);
            return roles.ToList();
        }

        public async Task<(ApplicationUser?, List<string>)> Login(string field, string password)
        {
            var user = await userManager.Users.Where(u => u.Email == field || u.PhoneNumber == field || u.IdentificationNumber == field)
                .FirstOrDefaultAsync();

            if (user == null) return (null, null);

            var isUser = await userManager.CheckPasswordAsync(user, password);

            if (!isUser) return (null, null);

            var roles = await GetRolesByUser(user);

            return (user, roles);
        }

        public async Task<ApplicationUser?> Register(ApplicationUser applicationUser, string password, List<string> roles)
        {
            applicationUser.UserName = applicationUser.IdentificationNumber;
            foreach (var role in roles)
            {
                var isRole = await roleManager.RoleExistsAsync(role);
                if (!isRole) return null;
            }
            var user  = await userManager.CreateAsync(applicationUser, password);

            var roleResult = await userManager.AddToRolesAsync(applicationUser, roles);
           
            if (roleResult == null)
            {
                await userManager.DeleteAsync(applicationUser);
                return null;
            }

            return applicationUser;
        }
    }
}
