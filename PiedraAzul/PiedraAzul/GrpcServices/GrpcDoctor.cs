using Grpc.Core;
using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Application.Features.Doctors;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Shared.Grpc;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcDoctor(IMediator mediator)
        : DoctorService.DoctorServiceBase
    {
        public override async Task<DoctorResponse> GetDoctor(DoctorRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.DoctorId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required."));
            }

            var doctor = await mediator.Send(new GetDoctorByUserIdQuery(request.DoctorId));

            if (doctor is null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Doctor not found."));
            }

            return MapToResponse(doctor);
        }

        public override async Task<DoctorListResponse> GetDoctorsByType(DoctorTypeRequest request, ServerCallContext context)
        {
            var doctors = await mediator.Send(
                new GetDoctorsBySpecialtyQuery(
                    (Domain.Entities.Shared.Enums.DoctorType)request.DoctorType));

            var response = new DoctorListResponse();

            response.Doctors.AddRange(doctors.Select(MapToResponse));

            return response;
        }

        private static DoctorResponse MapToResponse(DoctorDto d)
        {
            return new DoctorResponse
            {
                UserId = d.Id,
                DoctorId = d.Id,

                Name = d.Name,
                AvatarUrl = d.AvatarUrl,

                Specialty = (DoctorType)d.Specialty,
                LicenseNumber = d.LicenseNumber,
                Notes = d.Notes
            };
        }
    }
}