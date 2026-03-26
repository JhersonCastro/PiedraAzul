using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using PiedraAzul.Data.Models;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAppointment(
        PiedraAzul.ApplicationServices.Services.IAppointmentService appointmentService,
        PiedraAzul.ApplicationServices.Services.IPatientService patientService,
        IPatientAutocompleteService patientAutocompleteService)
        : AppointmentService.AppointmentServiceBase
    {
        public override async Task<AppointmentResponse> CreateAppointment(CreateAppointmentRequest request, ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date must be provided"));

            var normalizedDate = NormalizeToColombiaDayStartUtc(request.Date.ToDateTime());

            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required"));

            if (!Guid.TryParse(request.DoctorAvailabilitySlotId, out var slotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid slot ID"));

            string? patientUserId = null;

            if (Guid.TryParse(request.PatientId, out _))
            {
                patientUserId = request.PatientId;
            }
            else if (!string.IsNullOrWhiteSpace(request.PatientIdentification))
            {
                var guest = await patientService.GetPatientGuestById(request.PatientIdentification);

                if (guest == null)
                {
                    if (string.IsNullOrWhiteSpace(request.PatientName) ||
                        string.IsNullOrWhiteSpace(request.PatientPhone))
                    {
                        throw new RpcException(new Status(
                            StatusCode.InvalidArgument,
                            "Patient name and phone required for guest"
                        ));
                    }

                    var newGuest = new PatientGuest
                    {
                        PatientName = request.PatientName,
                        PatientPhone = request.PatientPhone,
                        PatientIdentification = request.PatientIdentification,
                        PatientExtraInfo = string.Empty
                    };

                    var result = await patientService.CreatePatientGuestAsync(newGuest);
                    await patientAutocompleteService.IndexGuestAsync(result);
                }
            }

            var appointment = new Appointment
            {
                DoctorUserId = request.DoctorId,
                DoctorAvailabilitySlotId = slotId,
                Date = normalizedDate
            };

            Appointment created;
            try
            {
                created = await appointmentService.CreateAppointmentAsync(
                    appointment,
                    patientUserId,
                    request.PatientIdentification
                );
            }
            catch (ArgumentException ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                               pg.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, "Appointment already exists for that slot/date."));
            }

            return new AppointmentResponse
            {
                Id = created.Id.ToString(),
                PatientId = created.PatientUserId ?? "",
                PatientGuestId = created.PatientGuestId ?? "",
                AppointmentSlotId = created.DoctorAvailabilitySlotId.ToString(),
                CreatedAt = Timestamp.FromDateTime(created.CreatedAt.ToUniversalTime())
            };
        }

        public override async Task<DoctorAppointmentsSearchResponse> GetDoctorAppointments(
            DoctorAppointmentsRequest request,
            ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required"));

            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date must be provided"));

            var date = request.Date.ToDateTime().ToUniversalTime();
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 50 : Math.Min(request.PageSize, 200);

            var search = await appointmentService.SearchDoctorAppointmentsAsync(
                request.DoctorId,
                date,
                pageNumber,
                pageSize
            );

            var response = new DoctorAppointmentsSearchResponse
            {
                TotalCount = search.TotalCount,
                PageNumber = search.PageNumber,
                PageSize = search.PageSize
            };

            response.Items.AddRange(search.Items.Select(a => new DoctorAppointmentItem
            {
                AppointmentId = a.AppointmentId.ToString(),
                TimeRange = a.TimeRange,
                Patient = a.Patient,
                PatientName = a.PatientName,
                PatientType = a.PatientType,
                Specialty = a.Specialty,
                Status = a.Status,
                Start = Timestamp.FromDateTime(DateTime.SpecifyKind(a.Start, DateTimeKind.Utc)),
                CreatedAt = Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime())
            }));

            return response;
        }

        private static DateTime NormalizeToColombiaDayStartUtc(DateTime date)
        {
            var colombiaTimeZone = ResolveColombiaTimeZone();
            var utcDate = date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime();
            var colombiaDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, colombiaTimeZone).Date;

            return TimeZoneInfo.ConvertTimeToUtc(colombiaDate, colombiaTimeZone);
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