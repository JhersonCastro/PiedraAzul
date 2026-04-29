using Mediator;
using PiedraAzul.Application.Common.Interfaces;

namespace PiedraAzul.Application.Features.Auth.Commands.MFA;

public class GenerateBackupCodesHandler : IRequestHandler<GenerateBackupCodesCommand, List<string>>
{
    private readonly IMFAService _mfaService;

    public GenerateBackupCodesHandler(IMFAService mfaService)
    {
        _mfaService = mfaService;
    }

    public async ValueTask<List<string>> Handle(GenerateBackupCodesCommand request, CancellationToken ct)
    {
        return await _mfaService.GenerateBackupCodesAsync(request.UserId);
    }
}
