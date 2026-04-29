using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.Login
{
    public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IIdentityService _identity;

        public LoginHandler(IIdentityService identity)
        {
            _identity = identity;
        }

        public async ValueTask<LoginResult> Handle(LoginCommand request, CancellationToken ct)
        {
            return await _identity.Login(request.Field, request.Password);
        }
    }
}