using PiedraAzul.Client.Models;
using PiedraAzul.Client.Wrappers;
using Shared.Grpc;

namespace PiedraAzul.Client.Services
{
    public class GrpcAvailability : AvailabilityService.AvailabilityServiceClient
    {
        private readonly AvailabilityService.AvailabilityServiceClient availabilityService;

        public GrpcAvailability(AvailabilityService.AvailabilityServiceClient availabilityService)
        {
            this.availabilityService = availabilityService;
        }


        public async Task<Result<AvailableSlotsResponse>> GetDoctorSlotsByDate(string doctorId, DateTime date, CancellationToken cancellationToken)
        {
            var result = await GrpcExecutor.Execute(async () => {
                var response = await availabilityService.GetAvailableSlotsAsync(new AvailableSlotsRequest
                {
                    DoctorId = doctorId,
                    Date = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(date.ToUniversalTime())
                }, cancellationToken:cancellationToken);
                return response;
            });

            return result;
        }
    }
}
