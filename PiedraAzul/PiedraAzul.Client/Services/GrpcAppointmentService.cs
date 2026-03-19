using Microsoft.JSInterop;
using Shared.Grpc ;

namespace PiedraAzul.Client.Services
{
    public class GrpcAppointmentService 
    {
        private readonly AppointmentService.AppointmentServiceClient appointmentClient;

        public GrpcAppointmentService(AppointmentService.AppointmentServiceClient appointmentClient)
        {
            this.appointmentClient = appointmentClient;
        }
    }
}
