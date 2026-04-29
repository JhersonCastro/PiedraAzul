using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public record BeginTOTPSetupCommand(string UserId, string Email) : IRequest<string>;
