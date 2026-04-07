using PiedraAzul.Application.Common.Models.User;

namespace PiedraAzul.Application.Common.Models.Auth
{
    public record RegisterResult(
        UserDto? User,
        List<string> Roles
    );
}