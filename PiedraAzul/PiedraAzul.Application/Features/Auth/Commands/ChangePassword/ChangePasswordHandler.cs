using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.ChangePassword;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, (bool Success, string? Error)>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<(bool Success, string? Error)> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        return await _identityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword);
    }
}
