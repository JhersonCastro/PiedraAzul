using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;
namespace PiedraAzul.Application.Features.Patients.Queries.SearchPatients;

public class SearchPatientsHandler
    : IRequestHandler<SearchPatientsQuery, List<PatientDto>>
{
    private readonly IPatientRepository _registered;
    private readonly IPatientGuestRepository _guest;

    public SearchPatientsHandler(
        IPatientRepository registered,
        IPatientGuestRepository guest)
    {
        _registered = registered;
        _guest = guest;
    }

    public async ValueTask<List<PatientDto>> Handle(
        SearchPatientsQuery request,
        CancellationToken ct)
    {
       var registered = await _registered.SearchAsync(request.Text, ct);
        var guests = await _guest.SearchAsync(request.Text, ct);

        return registered
            .Select(x => new PatientDto(x.Id, x.Name, "Registered"))
            .Concat(guests.Select(x => new PatientDto(x.Id, x.Name, "Guest")))
            .Take(10)
            .ToList();
    }
}