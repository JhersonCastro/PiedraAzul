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
    }
}