using Grpc.Core;
using PiedraAzul.Shared.Grpc;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcDoctor(PiedraAzul.ApplicationServices.Services.IDoctorService doctorService) : DoctorService.DoctorServiceBase
    {
        public override async Task<DoctorResponse> GetDoctor(DoctorRequest request, ServerCallContext context)
        {
            if (!Guid.TryParse(request.DoctorId, out Guid doctorId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid DoctorId format."));
            }

            var result = await doctorService.GetDoctorByIdAsync(doctorId);
            if (result == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Doctor not found."));
            }

            return new DoctorResponse
            {
                DoctorId = result.DoctorId.ToString(),
                UserId = result.UserId,
                Name = result.User.Name,
                Specialty = (DoctorType)result.Specialty,
                LicenseNumber = result.LicenseNumber,
                Notes = result.Notes,
                AvatarUrl = result.User.AvatarUrl
            };
        }
        public override Task<DoctorListResponse> GetDoctors(Empty request, ServerCallContext context)
        {
            return GetAllDoctorsAsync();
        }
        public override async Task<DoctorListResponse> GetDoctorsByType(DoctorTypeRequest request, ServerCallContext context)
        {
            var response = await doctorService.GetDoctorByTypeAsync((PiedraAzul.Shared.Enums.DoctorType)request.DoctorType);

            DoctorListResponse doctorListResponse= new DoctorListResponse();
            doctorListResponse.Doctors.AddRange(response.Select(d => new DoctorResponse
            {
                UserId = d.UserId,
                DoctorId = d.DoctorId.ToString(),
                LicenseNumber = d.LicenseNumber,
                Specialty = (DoctorType)d.Specialty,
                Name = d.User.Name,
                AvatarUrl = d.User.AvatarUrl,
                Notes = d.Notes,
            }));

            return doctorListResponse;
        }

        private async Task<DoctorListResponse> GetAllDoctorsAsync()
        {
            var doctors = await doctorService.GetAllDoctorsAsync();
            var response = new DoctorListResponse();
            response.Doctors.AddRange(doctors.Select(d => new DoctorResponse
            {
                UserId = d.UserId,
                DoctorId = d.DoctorId.ToString(),
                LicenseNumber = d.LicenseNumber,
                Specialty = (DoctorType)d.Specialty,
                Name = d.User.Name,
                AvatarUrl = d.User.AvatarUrl,
                Notes = d.Notes
            }));
            return response;
        }
    }
}
