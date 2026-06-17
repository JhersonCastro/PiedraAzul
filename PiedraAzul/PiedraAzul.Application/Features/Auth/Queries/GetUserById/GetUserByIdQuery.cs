using Mediator;
using PiedraAzul.Application.Common.Models.User;

namespace PiedraAzul.Application.Features.Users.Queries.GetUserById
{
    public record GetUserByIdQuery(string UserId) : IRequest<UserDto?>;
}