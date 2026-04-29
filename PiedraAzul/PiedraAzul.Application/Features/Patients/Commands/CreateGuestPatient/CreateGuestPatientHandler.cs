using Mediator;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Patients.Commands.CreateGuestPatient;

public class CreateGuestPatientHandler
    : IRequestHandler<CreateGuestPatientCommand, string>
{
    private readonly IPatientGuestRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGuestPatientHandler(
        IPatientGuestRepository repo,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<string> Handle(
        CreateGuestPatientCommand request,
        CancellationToken ct)
    {
        return await _unitOfWork.ExecuteAsync(async ct =>
        {
            var patient = new GuestPatient(
                Guid.NewGuid().ToString(),
                request.Name,
                request.Phone,
                request.ExtraInfo
            );

            await _repo.AddAsync(patient, ct);

            return patient.Id;
        }, ct);
    }
}