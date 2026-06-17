using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Account.Commands.RequestEmailChange;

public class RequestEmailChangeHandler : IRequestHandler<RequestEmailChangeCommand, bool>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public RequestEmailChangeHandler(
        IIdentityService identityService,
        IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async ValueTask<bool> Handle(RequestEmailChangeCommand request, CancellationToken ct)
    {
        // Create and send the verification code
        var (success, error) = await _identityService.RequestEmailChangeAsync(request.UserId, request.NewEmail);
        if (!success)
            return false;

        // Get the code from user (need to fetch user to get code)
        var user = await _identityService.GetUserByIdAsync(request.UserId);
        if (user is null)
            return false;

        var code = user.GetType().GetProperty("EmailChangeCode")?.GetValue(user) as string;
        if (string.IsNullOrEmpty(code))
            return false;

        var emailTemplate = $"""
            <h2>Cambio de correo electrónico</h2>
            <p>Hemos recibido una solicitud para cambiar tu correo a: <strong>{request.NewEmail}</strong></p>
            <p>Usa este código para verificar tu nuevo correo:</p>
            <p style="font-size: 24px; font-weight: bold; letter-spacing: 2px;">{code}</p>
            <p>Este código expira en 10 minutos.</p>
            <p>Si no realizaste esta solicitud, ignora este mensaje.</p>
            """;

        return await _emailService.SendGenericEmailAsync(
            request.NewEmail,
            "Verificación de cambio de correo - Piedra Azul",
            emailTemplate);
    }
}
