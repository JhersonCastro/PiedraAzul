using Mediator;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments
{
    public record GetPatientAppointmentsQuery(
    string? PatientUserId,
    string? PatientGuestId,
    DateOnly? Date = null
) : IRequest<IReadOnlyList<AppointmentDto>>;
}
