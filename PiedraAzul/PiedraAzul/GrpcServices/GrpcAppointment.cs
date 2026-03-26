using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using PiedraAzul.ApplicationServices.Mapping;
using PiedraAzul.Data.Models;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAppointment(
        PiedraAzul.ApplicationServices.Services.IAppointmentService appointmentService,
        PiedraAzul.ApplicationServices.Services.IPatientService patientService,
        IPatientAutocompleteService patientAutocompleteService,
        PatientMapper mapper)
        : AppointmentService.AppointmentServiceBase
    {
        public override async Task<AppointmentResponse> CreateAppointment(CreateAppointmentRequest request, ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date must be provided"));

            var dateUtc = request.Date.ToDateTime().ToUniversalTime();

            var normalizedDate = new DateTime(
                dateUtc.Year,
                dateUtc.Month,
                dateUtc.Day,
                0, 0, 0,
                DateTimeKind.Utc
            );

            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required"));

            if (!Guid.TryParse(request.DoctorAvailabilitySlotId, out var doctorAvailabilitySlotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid slot ID"));

            string? patientUserId = null;

            // Guid se usa porque si es un usario, userid es un guid, pero si es un invitado, es un string largo (numero de identificacion, o sea su cedula)
            if (Guid.TryParse(request.PatientId, out _))
            {
                // asumimos que es UserId válido
                patientUserId = request.PatientId;
            }
            else if (!string.IsNullOrWhiteSpace(request.PatientIdentification))
            {
                try
                {
                    var patientGuest = await patientService
                        .GetPatientGuestById(request.PatientIdentification);

                    if (patientGuest == null)
                    {
                        if (string.IsNullOrWhiteSpace(request.PatientName) ||
                            string.IsNullOrWhiteSpace(request.PatientPhone))
                        {
                            throw new RpcException(new Status(
                                StatusCode.InvalidArgument,
                                "Patient name and phone required for guest"
                            ));
                        }

                        var newPatientGuest = mapper.ToEntity(request);
                        var result = await patientService.CreatePatientGuestAsync(newPatientGuest);

                        if (result == null)
                            throw new RpcException(new Status(StatusCode.Internal, "Failed to create guest"));

                        await patientAutocompleteService.IndexGuestAsync(result);
                    }
                }
                catch (RpcException)
                {
                    throw;
                }
                catch
                {
                    throw new RpcException(new Status(
                        StatusCode.Internal,
                        "Unexpected error while handling patient guest"
                    ));
                }
            }

            var appointment = new Appointment
            {
                DoctorUserId = request.DoctorId,
                DoctorAvailabilitySlotId = doctorAvailabilitySlotId,
                Date = normalizedDate
            };

            try
            {
                var result = await appointmentService.CreateAppointmentAsync(
                    appointment,
                    patientUserId,
                    request.PatientIdentification
                );
                return new AppointmentResponse
                {
                    Id = result.Id.ToString(),
                    PatientId = result.PatientUserId ?? string.Empty,
                    PatientGuestId = result.PatientGuestId ?? string.Empty,
                    AppointmentSlotId = result.DoctorAvailabilitySlotId.ToString(),
                    Start = Timestamp.FromDateTime(result.Date.Add(result.DoctorAvailabilitySlot.StartTime).ToUniversalTime()),
                    CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp
                        .FromDateTime(result.CreatedAt.ToUniversalTime())
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                               pg.SqlState == "23505")
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists,
                    "This appointment slot is already taken"));
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.Internal,
                    "Error creating appointment"));
            }
        }

        public override async Task<AppointmentListResponse> GetDoctorAppointments(
    DoctorAppointmentsRequest request,
    ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            DateTime normalizedDate = default;

            if (request.Date != null)
            {
                var date = request.Date.ToDateTime();

                normalizedDate = new DateTime(
                    date.Year,
                    date.Month,
                    date.Day,
                    0, 0, 0,
                    DateTimeKind.Utc);
            }

            var appointments = await appointmentService
                .GetDoctorAppointmentsAsync(request.DoctorId, normalizedDate);

            var response = new AppointmentListResponse();

            response.Appointments.AddRange(appointments.Select(a =>
            {
                var combined = a.Date.Add(a.DoctorAvailabilitySlot.StartTime);

                var utcDateTime = DateTime.SpecifyKind(combined, DateTimeKind.Utc);

                return new AppointmentResponse
                {
                    Id = a.Id.ToString(),
                    PatientId = a.PatientUserId ?? string.Empty,
                    PatientGuestId = a.PatientGuestId ?? string.Empty,
                    PatientType = a.PatientUserId != null ? "Registered" : "Guest",
                    PatientName = a.PatientUserId != null
                        ? (a.Patient?.Name ?? a.Patient?.UserName ?? "Paciente registrado")
                        : (a.PatientGuest?.PatientName ?? "Paciente invitado"),
                    AppointmentSlotId = a.DoctorAvailabilitySlotId.ToString(),

                    Start = Google.Protobuf.WellKnownTypes.Timestamp
                        .FromDateTime(utcDateTime),

                    CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp
                        .FromDateTime(a.CreatedAt.ToUniversalTime())
                };
            }));

            return response;
        }

        public override async Task<AppointmentListResponse> GetPatientAppointments(PatientAppointmentsRequest request, ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (string.IsNullOrWhiteSpace(request.PatientId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Patient ID required"));

            List<Appointment> appointments;

            if(Guid.TryParse(request.PatientId, out _))
            {
                // asumimos que es UserId válido
                appointments = await appointmentService
                    .GetPatientAppointmentsAsync(request.PatientId, null);
            }
            else
            {
                appointments = await appointmentService
                    .GetPatientAppointmentsAsync(null, request.PatientId);
            }

            var response = new AppointmentListResponse();

            response.Appointments.AddRange(appointments.Select(a => new AppointmentResponse
            {
                Id = a.Id.ToString(),
                PatientId = a.PatientUserId ?? string.Empty,
                PatientGuestId = a.PatientGuestId ?? string.Empty,
                PatientType = a.PatientUserId != null ? "Registered" : "Guest",
                AppointmentSlotId = a.DoctorAvailabilitySlotId.ToString(),
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp
                    .FromDateTime(a.CreatedAt.ToUniversalTime())
            }));

            return response;
        }
    }
}