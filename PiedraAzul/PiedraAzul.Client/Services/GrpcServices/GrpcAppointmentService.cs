using Microsoft.JSInterop;
using PiedraAzul.Client.Models;
using PiedraAzul.Client.Services.Wrappers;
using Shared.Grpc;

namespace PiedraAzul.Client.Services.GrpcServices
{
    public class GrpcAppointmentService
    {
        private readonly AppointmentService.AppointmentServiceClient appointmentClient;

        public GrpcAppointmentService(AppointmentService.AppointmentServiceClient appointmentClient)
        {
            this.appointmentClient = appointmentClient;
        }

        public async Task<Result<AppointmentResponse>> CreateAppointment(CreateAppointmentRequest request)
        {
            var result = await GrpcExecutor.Execute(async () =>
            {
                var response = await appointmentClient.CreateAppointmentAsync(request);
                return response;
            });
            return result;
        }
    }
}
