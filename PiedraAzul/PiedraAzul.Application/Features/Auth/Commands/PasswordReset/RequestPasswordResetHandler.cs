using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.PasswordReset;

public class RequestPasswordResetHandler : IRequestHandler<RequestPasswordResetCommand, bool>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public RequestPasswordResetHandler(
        IIdentityService identityService,
        IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async ValueTask<bool> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        var resetToken = await _identityService.GeneratePasswordResetTokenAsync(request.Email);
        if (string.IsNullOrEmpty(resetToken))
            return false;

        var resetLink = $"https://piedraazul.runasp.net/account/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(resetToken)}";

        return await _emailService.SendPasswordResetEmailAsync(request.Email, request.Email, resetLink);
    }
}
