using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public class ConfirmTOTPSetupHandler : IRequestHandler<ConfirmTOTPSetupCommand, bool>
{
    private readonly IMFAService _mfaService;

    public ConfirmTOTPSetupHandler(IMFAService mfaService)
    {
        _mfaService = mfaService;
    }

    public async ValueTask<bool> Handle(ConfirmTOTPSetupCommand request, CancellationToken ct)
    {
        return await _mfaService.ConfirmTOTPSetupAsync(request.UserId, request.TOTP);
    }
}
