using Mediator;
using PiedraAzul.Application.Common.Models.Appointments;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Notifications
{
    /// <param name="AppointmentId">Id de la cita a recordar.</param>
    /// <param name="AppointmentStart">Fecha y hora exacta de la cita (UTC).</param>
    public sealed record AppointmentScheduleNotification(
        Guid AppointmentId,
        DateTime AppointmentStart) : INotification;
   
}
