using PiedraAzul.Application.Common.Models.User;

namespace PiedraAzul.Application.Common.Models.Auth
{
    public record LoginResult(
        UserDto? User,
        List<string> Roles
    );
}