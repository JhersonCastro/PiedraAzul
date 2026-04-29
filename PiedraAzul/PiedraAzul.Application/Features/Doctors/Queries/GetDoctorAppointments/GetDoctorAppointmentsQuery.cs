using Mediator;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments
{
    public record GetDoctorAppointmentsQuery(
    string DoctorId,
    DateOnly? Date = null
) : IRequest<IReadOnlyList<AppointmentDto>>;
}
