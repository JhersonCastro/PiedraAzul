using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty
{
    public record GetDoctorsBySpecialtyQuery(DoctorType Specialty, bool OnlyAvailable = false)
       : IRequest<IReadOnlyList<DoctorDto>>;
}
