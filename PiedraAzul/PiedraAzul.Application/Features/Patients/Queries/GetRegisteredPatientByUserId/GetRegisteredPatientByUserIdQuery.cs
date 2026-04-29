using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.GetRegisteredPatientByUserId
{
    public record GetRegisteredPatientByUserIdQuery(string UserId)
    : IRequest<PatientDto?>;
}
