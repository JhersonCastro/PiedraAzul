using Mediator;

namespace PiedraAzul.Application.Features.Users.Queries.GetUserRoles
{
    public record GetUserRolesQuery(string UserId) : IRequest<List<string>>;
}