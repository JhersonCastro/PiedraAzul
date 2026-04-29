using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.User;

namespace PiedraAzul.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IIdentityService _identity;

        public GetUserByIdHandler(IIdentityService identity)
        {
            _identity = identity;
        }

        public async ValueTask<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            return await _identity.GetById(request.UserId);
        }
    }
}