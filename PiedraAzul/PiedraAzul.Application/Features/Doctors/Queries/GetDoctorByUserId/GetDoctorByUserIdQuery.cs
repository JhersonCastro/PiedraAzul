using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId
{
    public record GetDoctorByUserIdQuery(string UserId) : IRequest<DoctorDto?>;
}
