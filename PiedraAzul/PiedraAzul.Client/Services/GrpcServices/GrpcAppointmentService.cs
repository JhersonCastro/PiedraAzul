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
            var utcDate = ConvertColombiaDateToUtc(date);

            var request = new DoctorAppointmentsRequest
            {
                DoctorId = doctorId,
                Date = Google.Protobuf.WellKnownTypes.Timestamp
                    .FromDateTime(utcDate),
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await GrpcExecutor.Execute(async () =>
            {
                return await appointmentClient.GetDoctorAppointmentsAsync(request);
            });
        }

        private static DateTime ConvertColombiaDateToUtc(DateTime date)
        {
            var colombiaTimeZone = ResolveColombiaTimeZone();
            var localDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(localDate, colombiaTimeZone);
        }

        private static TimeZoneInfo ResolveColombiaTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            }
        }
    }
}