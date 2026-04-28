using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Account.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeHandler : IRequestHandler<ConfirmEmailChangeCommand, bool>
{
    private readonly IIdentityService _identityService;

    public ConfirmEmailChangeHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<bool> Handle(ConfirmEmailChangeCommand request, CancellationToken ct)
    {
        return await _identityService.ConfirmEmailChangeAsync(request.UserId, request.NewEmail, request.Code);
    }
}
