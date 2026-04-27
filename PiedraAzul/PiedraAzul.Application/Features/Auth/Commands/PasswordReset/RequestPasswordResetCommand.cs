using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.PasswordReset;

public record RequestPasswordResetCommand(string Email) : IRequest<bool>;
