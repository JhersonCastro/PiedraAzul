using Shared.Grpc;

namespace PiedraAzul.Client.Services.GrpcServices
{
public class GrpcDoctorService(DoctorService.DoctorServiceClient doctorClient)
    {
        public async Task<List<DoctorResponse>> GetDoctorsAsync()
        {
            var response = await doctorClient.GetDoctorsAsync(new PiedraAzul.Shared.Grpc.Empty());
            return response.Doctors.ToList();
        }

        public async Task<List<DoctorResponse>> GetDoctorsByTypeAsync(Shared.Enums.DoctorType doctorType)
        {
            var request = new DoctorTypeRequest { DoctorType = (DoctorType)doctorType };
            var response = await doctorClient.GetDoctorsByTypeAsync(request);
            return response.Doctors.ToList();
        }
    }
}