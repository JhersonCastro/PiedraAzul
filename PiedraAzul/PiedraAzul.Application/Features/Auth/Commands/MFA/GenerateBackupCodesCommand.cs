using Mediator;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public record GenerateBackupCodesCommand(string UserId) : IRequest<List<string>>;
