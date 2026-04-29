using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public record ConfirmTOTPSetupCommand(string UserId, string TOTP) : IRequest<bool>;
