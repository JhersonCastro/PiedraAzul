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

        public async Task<Result<DoctorAppointmentsSearchResponse>> GetDoctorAppointmentsAsync(
            string doctorUserId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50)
        {
            var request = new DoctorAppointmentsRequest
            {
                DoctorId = doctorUserId,
                Date = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc)),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await GrpcExecutor.Execute(async () =>
            {
                var response = await appointmentClient.GetDoctorAppointmentsAsync(request);
                return response;
            });

            return result;
        }
    }
}