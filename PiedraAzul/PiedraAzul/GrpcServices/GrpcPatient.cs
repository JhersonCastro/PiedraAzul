using Grpc.Core;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcPatient(
        PiedraAzul.ApplicationServices.Services.IPatientService patientService,
        IPatientAutocompleteService patientAutocompleteService)
        : PatientService.PatientServiceBase
    {

        public override async Task<SearchPatientsResponse> SearchAutoCompletePatients(SearchPatientsRequest request, ServerCallContext context)
        {
            var patient = await patientAutocompleteService.SearchAsync(request.Query, request.Limit);

            var response = new SearchPatientsResponse();

            response.Patients.AddRange(patient.Select(p => new PatientSearchResult
            {
                Id = p.Id,
                Identification = p.Identification,
                Name = p.Name,
                Phone = p.Phone,
                Type = p.EntityType == "Guest"
                    ? PatientType.Guest
                    : PatientType.Registered
            }));

            return response;
        }

        public override async Task<SearchPatientsResponse> SearchPatients(
            SearchPatientsRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return new SearchPatientsResponse();
            }

            var patientProfiles = await patientService.GetPatientProfileByQueryAsync(request.Query);
            var patientGuests = await patientService.GetPatientGuestByQuery(request.Query);

            var combined = patientProfiles.Select(p => new PatientSearchResult
            {
                // 🔥 CAMBIO CLAVE AQUÍ
                Id = p.UserId, // antes: p.PatientId ❌

                Identification = p.User?.IdentificationNumber ?? string.Empty,
                Name = p.User?.Name ?? string.Empty,
                Phone = p.User?.PhoneNumber ?? string.Empty,
                Type = PatientType.Registered
            })
            .Concat(patientGuests.Select(p => new PatientSearchResult
            {
                Id = p.PatientIdentification ?? string.Empty,
                Identification = p.PatientIdentification ?? string.Empty,
                Name = p.PatientName ?? string.Empty,
                Phone = p.PatientPhone ?? string.Empty,
                Type = PatientType.Guest
            }));

            combined = combined
                .GroupBy(p => p.Identification)
                .Select(g => g.First());

            combined = combined
                .OrderBy(p => p.Name);

            if (request.Limit > 0)
            {
                combined = combined.Take(request.Limit);
            }

            var response = new SearchPatientsResponse();
            response.Patients.AddRange(combined);

            return response;
        }
    }
}