using Shared.Grpc;

namespace PiedraAzul.Client.Services
{
    public class GrpcDoctorService(DoctorService.DoctorServiceClient doctorClient)
    {
        public async Task<List<DoctorResponse>> GetDoctorsByTypeAsync(PiedraAzul.Shared.Enums.DoctorType doctorType)
        {
            var request = new DoctorTypeRequest { DoctorType = (DoctorType)doctorType };
            var response = await doctorClient.GetDoctorsByTypeAsync(request);
            return response.Doctors.ToList();
        }
    }
}
