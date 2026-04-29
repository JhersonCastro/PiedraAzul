using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.GetRegisteredPatientByUserId
{
    public class GetRegisteredPatientByUserIdHandler
    : IRequestHandler<GetRegisteredPatientByUserIdQuery, PatientDto?>
    {
        private readonly IPatientRepository _repo;

        public GetRegisteredPatientByUserIdHandler(IPatientRepository repo)
        {
            _repo = repo;
        }

        public async ValueTask<PatientDto?> Handle(
            GetRegisteredPatientByUserIdQuery request,
            CancellationToken ct)
        {
            var patient = await _repo.GetByUserIdAsync(request.UserId, ct);

            if (patient == null) return null;

            return new PatientDto(patient.Id, patient.Name, "Registered");
        }
    }
}
