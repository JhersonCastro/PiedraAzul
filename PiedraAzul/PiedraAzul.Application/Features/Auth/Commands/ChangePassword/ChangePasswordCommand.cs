using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.ChangePassword;

public record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword) : IRequest<(bool Success, string? Error)>;
