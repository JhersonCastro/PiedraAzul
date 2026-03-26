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
            return await GrpcExecutor.Execute(async () =>
            {
                return await appointmentClient.CreateAppointmentAsync(request);
            });
        }

        public async Task<Result<DoctorAppointmentsSearchResponse>> GetDoctorAppointments(
            string doctorId,
            DateTime date,
            int pageNumber = 1,
            int pageSize = 50)
        {
            var request = new DoctorAppointmentsRequest
            {
                DoctorId = doctorId,
                Date = Google.Protobuf.WellKnownTypes.Timestamp
                    .FromDateTime(DateTime.SpecifyKind(date, DateTimeKind.Utc)),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GrpcExecutor.Execute(async () =>
            {
                return await appointmentClient.GetDoctorAppointmentsAsync(request);
            });
        }
    }
}