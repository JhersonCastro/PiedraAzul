using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId
{
    public class GetDoctorByUserIdHandler : IRequestHandler<GetDoctorByUserIdQuery, DoctorDto?>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IIdentityService _identityService;

        public GetDoctorByUserIdHandler(
            IDoctorRepository doctorRepository,
            IIdentityService identityService)
        {
            _doctorRepository = doctorRepository;
            _identityService = identityService;
        }

        public async ValueTask<DoctorDto?> Handle(
            GetDoctorByUserIdQuery request,
            CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (doctor is null)
                return null;

            var user = await _identityService.GetById(request.UserId);

            if (user is null)
                return null;

            return new DoctorDto
            {
                Id = doctor.Id,
                Name = user.Name,
                AvatarUrl = user.AvatarUrl,

                Specialty = doctor.Specialty,
                LicenseNumber = doctor.LicenseNumber,
                Notes = doctor.Notes
            };
        }
    }
}