using Mediator;

namespace PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole
{
    public record CreateProfileForRoleCommand(string UserId, string Role) : IRequest;
}