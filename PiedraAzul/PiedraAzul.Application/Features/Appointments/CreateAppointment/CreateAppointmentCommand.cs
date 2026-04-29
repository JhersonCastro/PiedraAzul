using Mediator;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Appointments.CreateAppointment
{
    public record CreateAppointmentCommand(
    string DoctorId,
    Guid SlotId,
    DateOnly Date,

    string? PatientUserId,
    GuestPatientRequest? PatientGuest
) : IRequest<Appointment>;
}
