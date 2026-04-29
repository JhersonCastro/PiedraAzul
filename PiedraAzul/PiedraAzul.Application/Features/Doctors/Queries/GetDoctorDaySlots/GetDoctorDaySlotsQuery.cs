using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots
{
    public sealed record GetDoctorDaySlotsQuery(
    string DoctorId,
    DateOnly Date
) : IRequest<IReadOnlyList<DoctorSlotAvailabilityDto>>;
}
