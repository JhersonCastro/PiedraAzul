using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public class VerifyMFALoginHandler : IRequestHandler<VerifyMFALoginCommand, LoginResult>
{
    private readonly IMFATokenService _mfaTokenService;
    private readonly IMFAService _mfaService;
    private readonly IIdentityService _identityService;

    public VerifyMFALoginHandler(
        IMFATokenService mfaTokenService,
        IMFAService mfaService,
        IIdentityService identityService)
    {
        _mfaTokenService = mfaTokenService;
        _mfaService = mfaService;
        _identityService = identityService;
    }

    public async ValueTask<LoginResult> Handle(VerifyMFALoginCommand request, CancellationToken ct)
    {
        var userId = _mfaTokenService.ValidateMFAToken(request.MFAToken);
        if (string.IsNullOrEmpty(userId))
            return new LoginResult(null, []);

        var method = await _mfaService.GetMFAMethodAsync(userId);

        bool isValid = method switch
        {
            "TOTP" => await _mfaService.VerifyTOTPAsync(userId, request.OTP),
            "Email" => await _mfaService.VerifyOTPAsync(userId, request.OTP),
            _ => await _mfaService.VerifyBackupCodeAsync(userId, request.OTP)
        };

        if (!isValid)
            return new LoginResult(null, []);

        var user = await _identityService.GetById(userId);
        if (user is null)
            return new LoginResult(null, []);

        var roles = await _identityService.GetRolesByUser(userId);
        return new LoginResult(user, roles);
    }
}
