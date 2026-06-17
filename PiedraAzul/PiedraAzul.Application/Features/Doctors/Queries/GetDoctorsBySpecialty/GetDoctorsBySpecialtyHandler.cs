using Mediator;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty
{
    public class GetDoctorsBySpecialtyHandler
        : IRequestHandler<GetDoctorsBySpecialtyQuery, IReadOnlyList<DoctorDto>>
    {
        private readonly IDoctorRepository _doctorRepository;
        private readonly IIdentityService _identityService;

        public GetDoctorsBySpecialtyHandler(
            IDoctorRepository doctorRepository,
            IIdentityService identityService)
        {
            _doctorRepository = doctorRepository;
            _identityService = identityService;
        }

        public async ValueTask<IReadOnlyList<DoctorDto>> Handle(
            GetDoctorsBySpecialtyQuery request,
            CancellationToken cancellationToken)
        {
            var doctors = await _doctorRepository
                .GetBySpecialtyAsync(request.Specialty, cancellationToken);

            if (doctors.Count == 0)
                return [];

            var userIds = doctors.Select(d => d.Id).ToList();

            var users = await _identityService.GetByIds(userIds);

            var userDict = users.ToDictionary(u => u.Id);

            var result = doctors
                .Where(d => userDict.ContainsKey(d.Id))
                .Select(d =>
                {
                    var user = userDict[d.Id];

                    return new DoctorDto
                    {
                        Id = d.Id,
                        Name = user.Name,
                        AvatarUrl = user.AvatarUrl,

                        Specialty = d.Specialty,
                        LicenseNumber = d.LicenseNumber,
                        Notes = d.Notes
                    };
                })
                .ToList();

            return result;
        }
    }
}