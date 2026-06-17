using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public class BeginTOTPSetupHandler : IRequestHandler<BeginTOTPSetupCommand, string>
{
    private readonly IMFAService _mfaService;

    public BeginTOTPSetupHandler(IMFAService mfaService)
    {
        _mfaService = mfaService;
    }

    public async ValueTask<string> Handle(BeginTOTPSetupCommand request, CancellationToken ct)
    {
        await _mfaService.GenerateTOTPSecretAsync(request.UserId);
        var qrCode = await _mfaService.GetTOTPQRCodeAsync(request.UserId, request.Email);
        return qrCode;
    }
}
