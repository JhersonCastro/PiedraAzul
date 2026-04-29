using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Users.Queries.GetUserRoles
{
    public class GetUserRolesHandler : IRequestHandler<GetUserRolesQuery, List<string>>
    {
        private readonly IIdentityService _identity;

        public GetUserRolesHandler(IIdentityService identity)
        {
            _identity = identity;
        }

        public async ValueTask<List<string>> Handle(GetUserRolesQuery request, CancellationToken ct)
        {
            return await _identity.GetRolesByUser(request.UserId);
        }
    }
}