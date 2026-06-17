using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole
{
    public class CreateProfileForRoleHandler : IRequestHandler<CreateProfileForRoleCommand>
    {
        private readonly IIdentityService _identity;

        public CreateProfileForRoleHandler(IIdentityService identity)
        {
            _identity = identity;
        }

        public async ValueTask<Unit> Handle(CreateProfileForRoleCommand request, CancellationToken ct)
        {
            await _identity.CreateProfileForRoleAsync(request.UserId, request.Role);
            return Unit.Value;
        }
    }
}