using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.GetGuestById
{
    public record GetGuestPatientByIdQuery(string Id)
    : IRequest<PatientDto?>;
}
