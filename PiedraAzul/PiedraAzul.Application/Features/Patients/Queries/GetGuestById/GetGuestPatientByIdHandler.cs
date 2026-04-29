using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.GetGuestById
{
    public class GetGuestPatientByIdHandler
    : IRequestHandler<GetGuestPatientByIdQuery, PatientDto?>
    {
        private readonly IPatientGuestRepository _repo;

        public GetGuestPatientByIdHandler(IPatientGuestRepository repo)
        {
            _repo = repo;
        }

        public async ValueTask<PatientDto?> Handle(
            GetGuestPatientByIdQuery request,
            CancellationToken ct)
        {
            var patient = await _repo.GetByIdAsync(request.Id, ct);

            if (patient == null) return null;

            return new PatientDto(patient.Id, patient.Name, "Guest");
        }
    }
}
