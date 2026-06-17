using Mediator;

namespace PiedraAzul.Application.Features.Account.Commands.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(string UserId, string NewEmail, string Code) : IRequest<bool>;
