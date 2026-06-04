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
            // Usar la cédula como ID para que GetByIdAsync(cédula) pueda encontrar
            // pacientes existentes y no crear duplicados en visitas futuras.
            var id = !string.IsNullOrWhiteSpace(request.IdentificationId)
                ? request.IdentificationId
                : Guid.NewGuid().ToString();

            var patient = new GuestPatient(
                id,
                request.Name,
                request.Phone,
                request.ExtraInfo,
                request.Email,
                request.DocumentType
            );

            await _repo.AddAsync(patient, ct);

            return patient.Id;
        }, ct);
    }
}