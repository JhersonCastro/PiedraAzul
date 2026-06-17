using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.PasswordReset;

public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<bool>;
