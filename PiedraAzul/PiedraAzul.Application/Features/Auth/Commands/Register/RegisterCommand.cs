using Mediator;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.Register
{
    public record RegisterCommand(
        RegisterUserDto User,
        string Password,
        List<string> Roles
    ) : IRequest<RegisterResult>;
}