using PiedraAzul.Domain.Entities.Profiles.Doctor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Common.Models.Doctor
{
    public record DoctorSlotAvailabilityDto(
    Guid Id,
    TimeSpan StartTime,
    TimeSpan EndTime,
    bool IsAvailable
);
}
