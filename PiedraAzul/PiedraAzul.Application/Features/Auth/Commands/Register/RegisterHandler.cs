using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.Register
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, RegisterResult>
    {
        private readonly IIdentityService _identity;

        public RegisterHandler(IIdentityService identity)
        {
            _identity = identity;
        }

        public async ValueTask<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
        {
            return await _identity.Register(request.User, request.Password, request.Roles);
        }
    }
}