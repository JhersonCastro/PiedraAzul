using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.PasswordReset;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<bool> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        return await _identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
    }
}
