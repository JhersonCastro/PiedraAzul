using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.SearchPatients
{
    public record SearchPatientsQuery(string Text)
    : IRequest<List<PatientDto>>;
}
