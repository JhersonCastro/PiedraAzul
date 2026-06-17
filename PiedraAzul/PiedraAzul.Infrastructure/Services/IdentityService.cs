using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
    RoleManager<IdentityRole> roleManager,
    IMemoryCache cache,
    IEmailService emailService
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

        // Check if account is locked out
        if (await userManager.IsLockedOutAsync(user))
            return new LoginResult(null, []);

        var isValid = await userManager.CheckPasswordAsync(user, password);
        if (!isValid)
        {
            await userManager.AccessFailedAsync(user);
            return new LoginResult(null, []);
        }

        // Reset failed attempts on successful login
        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return new LoginResult(ToDto(user), roles.ToList());
    }

    public async Task<RegisterResult> Register(RegisterUserDto dto, string password, List<string> roles)
    {
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                return new RegisterResult(null, [], "Rol inválido");
        }

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var existingUser = await userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
                return new RegisterResult(null, [], "Este correo ya está registrado");
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
        {
            var errorMessage = createResult.Errors.FirstOrDefault()?.Description ?? "No se pudo crear la cuenta";
            return new RegisterResult(null, [], errorMessage);
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return new RegisterResult(null, [], "No se pudieron asignar los roles");
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
                u.AvatarUrl,
                u.EmailConfirmed
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
                u.AvatarUrl,
                u.EmailConfirmed
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

    public async Task<string?> GeneratePasswordResetTokenAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        return await userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return false;

        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    public async Task<object?> GetUserByIdAsync(string userId)
    {
        return await userManager.FindByIdAsync(userId);
    }

    public async Task<object?> GetUserByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }

    public async Task<bool> UpdateUserAsync(object user)
    {
        if (user is not ApplicationUser appUser)
            return false;

        var result = await userManager.UpdateAsync(appUser);
        return result.Succeeded;
    }

    public async Task<(bool Success, string? Error)> RequestEmailChangeAsync(string userId, string newEmail)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, "Usuario no encontrado");

        // Check if new email is the same as current (normalize for comparison)
        if (!string.IsNullOrEmpty(user.Email) && user.Email.Equals(newEmail, StringComparison.OrdinalIgnoreCase))
            return (false, "El nuevo correo es igual al actual");

        // Check if email already exists
        var existingUser = await userManager.FindByEmailAsync(newEmail);
        if (existingUser is not null && existingUser.Id != userId)
            return (false, "Este correo ya está en uso");

        var code = Random.Shared.Next(100000, 999999).ToString();
        user.SetEmailChangeToken(newEmail, code);
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded ? (true, null) : (false, "Error al procesar la solicitud");
    }

    public async Task<bool> ConfirmEmailChangeAsync(string userId, string newEmail, string code)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        if (!user.HasPendingEmailChange(newEmail))
            return false;

        if (!user.VerifyEmailChangeCode(code))
            return false;

        user.Email = newEmail;
        user.NormalizedEmail = newEmail.ToUpper();
        user.EmailConfirmed = false;
        user.ClearEmailChangeToken();

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SendEmailVerificationCodeAsync(string userId, string email)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        var code = Random.Shared.Next(100000, 999999).ToString();
        cache.Set($"email_verify_{userId}", code, TimeSpan.FromMinutes(10));

        var emailSent = await emailService.SendMFAEmailAsync(
            email,
            user.Name ?? email,
            $"Tu código de verificación es: {code}",
            10
        );

        return emailSent;
    }

    public async Task<bool> VerifyEmailCodeAsync(string userId, string code)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return false;

        var cacheKey = $"email_verify_{userId}";
        if (!cache.TryGetValue(cacheKey, out string? storedCode))
            return false;

        if (storedCode != code)
            return false;

        cache.Remove(cacheKey);
        user.EmailConfirmed = true;
        var result = await userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    private static UserDto ToDto(ApplicationUser user)
    {
        return new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.Name,
            user.AvatarUrl,
            user.EmailConfirmed
        );
    }
}