using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.User;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;

namespace PiedraAzul.Infrastructure.Services;

public class IdentityService(
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager
) : IIdentityService
{
    public async Task<LoginResult> Login(string field, string password)
    {
        var user = await userManager.Users
            .FirstOrDefaultAsync(u =>
                u.Email == field ||
                u.PhoneNumber == field ||
                u.IdentificationNumber == field);

        if (user is null)
            return new LoginResult(null, []);

        var isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
            return new LoginResult(null, []);

        var roles = await userManager.GetRolesAsync(user);
        return new LoginResult(ToDto(user), roles.ToList());
    }

    public async Task<RegisterResult> Register(RegisterUserDto dto, string password, List<string> roles)
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                return new RegisterResult(null, []);
        }

        var user = new ApplicationUser
        {
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            IdentificationNumber = dto.IdentificationNumber,
            UserName = dto.IdentificationNumber ?? dto.Email,
            Name = dto.Name,
            AvatarUrl = "default.png"
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return new RegisterResult(null, []);

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return new RegisterResult(null, []);
        }

        return new RegisterResult(ToDto(user), roles);
    }

    public async Task<List<string>> GetRolesByUser(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return [];

        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<UserDto?> GetById(string userId)
    {
        return await userManager.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto(
                u.Id,
                u.Email ?? string.Empty,
                u.Name,
                u.AvatarUrl
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserDto>> GetByIds(List<string> userIds)
    {
        if (userIds.Count == 0)
            return [];

        var ids = new HashSet<string>(userIds);

        return await userManager.Users
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserDto(
                u.Id,
                u.Email ?? string.Empty,
                u.Name,
                u.AvatarUrl
            ))
            .ToListAsync();
    }

    public async Task CreateProfileForRoleAsync(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or empty", nameof(role));

        var normalizedRole = role.Trim().ToLowerInvariant();

        switch (normalizedRole)
        {
            case "patient":
                {
                    var exists = await context.Patients
                        .OfType<RegisteredPatient>()
                        .AnyAsync(p => p.UserId == userId);

                    if (exists) return;

                    var user = await userManager.FindByIdAsync(userId);
                    var name = user?.Name ?? user?.UserName ?? string.Empty;

                    await context.Patients.AddAsync(new RegisteredPatient(userId, name));
                    break;
                }

            case "doctor":
                throw new InvalidOperationException(
                    "Doctor cannot be created from auth alone.");

            default:
                throw new InvalidOperationException(
                    $"Role '{role}' does not have a corresponding domain entity.");
        }

        await context.SaveChangesAsync();
    }

    public async Task<UserDto?> UpdateProfileAsync(string userId, string name, string? avatarUrl)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return null;

        user.Name = name;
        if (!string.IsNullOrWhiteSpace(avatarUrl))
            user.AvatarUrl = avatarUrl;

        await userManager.UpdateAsync(user);
        return ToDto(user);
    }

    private static UserDto ToDto(ApplicationUser user)
    {
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Name,
            user.AvatarUrl
        );
    }
}