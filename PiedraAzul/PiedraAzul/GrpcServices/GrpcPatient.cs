using Grpc.Core;
using Mediator;
using PiedraAzul.Application.Features.Patients.Queries.SearchPatients;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcPatient(IMediator mediator)
        : PatientService.PatientServiceBase
    {
        // =========================
        // AUTOCOMPLETE
        // =========================
        public override async Task<SearchPatientsResponse> SearchAutoCompletePatients(
            SearchPatientsRequest request,
            ServerCallContext context)
        {
            var patients = await mediator.Send(
                new SearchPatientsQuery(request.Query)
            );

            var response = new SearchPatientsResponse();

            response.Patients.AddRange(
                patients.Select(p => new PatientSearchResult
                {
                    Id = p.Id,
                    Identification = p.Id,
                    Name = p.Name,
                    Phone = "",
                    Type = p.Type == "Guest"
                        ? PatientType.Guest
                        : PatientType.Registered
                })
            );

            return response;
        }

        // =========================
        // SEARCH COMPLETO
        // =========================
        public override async Task<SearchPatientsResponse> SearchPatients(
            SearchPatientsRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return new SearchPatientsResponse();

            var patients = await mediator.Send(
                new SearchPatientsQuery(request.Query)
            );

            var combined = patients
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderBy(p => p.Name)
                .Take(request.Limit > 0 ? request.Limit : int.MaxValue)
                .ToList(); 

            var response = new SearchPatientsResponse();

            response.Patients.AddRange(
                combined.Select(p => new PatientSearchResult
                {
                    Id = p.Id,
                    Identification = p.Id,
                    Name = p.Name,
                    Phone = "",
                    Type = p.Type == "Guest"
                        ? PatientType.Guest
                        : PatientType.Registered
                })
            );

            return response;
        }
    }
}