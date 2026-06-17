using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.User;

namespace PiedraAzul.Application.Common.Interfaces
{
    public interface IIdentityService
    {

        Task<LoginResult> Login(string field, string password);
        Task<RegisterResult> Register(RegisterUserDto user, string password, List<string> roles);
        Task<List<string>> GetRolesByUser(string userId);
        Task<UserDto?> GetById(string userId);
        Task<List<UserDto>> GetByIds(List<string> userIds);
        Task CreateProfileForRoleAsync(string userId, string role);
        Task<UserDto?> UpdateProfileAsync(string userId, string name, string? avatarUrl);
        Task<string?> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
        Task<object?> GetUserByIdAsync(string userId);
        Task<object?> GetUserByEmailAsync(string email);
        Task<bool> UpdateUserAsync(object user);
        Task<(bool Success, string? Error)> RequestEmailChangeAsync(string userId, string newEmail);
        Task<bool> ConfirmEmailChangeAsync(string userId, string newEmail, string code);
        Task<bool> SendEmailVerificationCodeAsync(string userId, string email);
        Task<bool> VerifyEmailCodeAsync(string userId, string code);
    }
}