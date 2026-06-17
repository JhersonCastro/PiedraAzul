using Mediator;

namespace PiedraAzul.Application.Features.Account.Commands.RequestEmailChange;

public record RequestEmailChangeCommand(string UserId, string NewEmail) : IRequest<bool>;
