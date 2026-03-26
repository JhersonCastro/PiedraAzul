using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using PiedraAzul.ApplicationServices.Mapping;
using PiedraAzul.Data.Models;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAppointment(PiedraAzul.ApplicationServices.Services.IAppointmentService appointmentService,
        PiedraAzul.ApplicationServices.Services.IPatientService patientService, IPatientAutocompleteService patientAutocompleteService, PatientMapper mapper)
        : AppointmentService.AppointmentServiceBase
    {
        public override async Task<AppointmentResponse> CreateAppointment(CreateAppointmentRequest request, ServerCallContext context)
        {
            if (request == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            var dateUtc = request.Date.ToDateTime().ToUniversalTime();

            // Truncar a medianoche (00:00:00)
            var normalizedDate = new DateTime(
                dateUtc.Year,
                dateUtc.Month,
                dateUtc.Day,
                0, 0, 0,
                DateTimeKind.Utc
            );


            if (!Guid.TryParse(request.DoctorId, out var doctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            if (!Guid.TryParse(request.DoctorAvailabilitySlotId, out var doctorAvailabilitySlotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor availability slot ID format"));

            // if the patientid is not provided, we can create a new patient record using the provided patient information, but we need almost the patient identification
            string? patientId = null;

            if (Guid.TryParse(request.PatientId, out _))
            {
                patientId = request.PatientId;
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
                                "Patient name and phone must be provided to create a new patient guest record"
                            ));
                        }
                        var newPatientGuest = mapper.ToEntity(request);

                        var result = await patientService.CreatePatientGuestAsync(newPatientGuest);

                        if (result == null)
                        {
                            throw new RpcException(new Status(
                                StatusCode.Internal,
                                "Failed to create patient guest"
                            ));
                        }

                        await patientAutocompleteService.IndexGuestAsync(result);
                    }
                }
                catch (RpcException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new RpcException(new Status(
                        StatusCode.Internal,
                        "Unexpected error while handling patient guest"
                    ));
                }
            }


            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date must be provided"));

            //Map the gRPC request to the domain model
            Appointment appointment = new Appointment
            {
                DoctorId = doctorId,
                DoctorAvailabilitySlotId = doctorAvailabilitySlotId,
                Date = normalizedDate,
            };

            try
            {
                var result = await appointmentService.CreateAppointmentAsync(appointment, patientId, request.PatientIdentification);


                return new AppointmentResponse
                {
                    Id = result.Id.ToString(),
                    PatientId = result.PatientId?.ToString() ?? string.Empty,
                    AppointmentSlotId = result.DoctorAvailabilitySlotId.ToString(),
                    CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(result.CreatedAt.ToUniversalTime())
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg &&
                                   pg.SqlState == "23505") // unique_violation
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists,
                    "This appointment slot is already taken"));
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while creating the appointment"));
            }

        }
        public override async Task<DoctorAppointmentsSearchResponse> GetDoctorAppointments(DoctorAppointmentsRequest request, ServerCallContext context)
        {
            if (request == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));
            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Doctor ID is required"));
            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date is required"));

            var date = request.Date.ToDateTime().ToUniversalTime();
            var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            var pageSize = request.PageSize <= 0 ? 50 : request.PageSize;

            var search = await appointmentService.SearchDoctorAppointmentsAsync(request.DoctorId, date, pageNumber, pageSize);

            DoctorAppointmentsSearchResponse response = new DoctorAppointmentsSearchResponse
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
                PatientType = a.PatientType,
                Specialty = a.Specialty,
                Status = a.Status,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime())
            }));

            return response;
        }
        public override async Task<AppointmentListResponse> GetPatientAppointments(PatientAppointmentsRequest request, ServerCallContext context)
        {
            if (request == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));
            if(string.IsNullOrWhiteSpace(request.PatientId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Patient ID must be provided"));

            List<Appointment> appointments;
            if (Guid.TryParse(request.PatientId, out var patientId))
            {
                appointments = await appointmentService.GetPatientAppointmentsAsync(request.PatientId, string.Empty);
            }
            else {
                appointments = await appointmentService.GetPatientAppointmentsAsync(string.Empty, request.PatientId);
            }

            if (appointments == null) throw new RpcException(new Status(StatusCode.NotFound, "No appointments found for the given patient ID"));

            AppointmentListResponse response = new AppointmentListResponse();

            response.Appointments.AddRange(appointments.Select(a => new AppointmentResponse
            {
                Id = a.Id.ToString(),
                PatientId = a.PatientId?.ToString() ?? string.Empty,
                PatientGuestId = a.PatientGuestId?.ToString() ?? string.Empty,
                PatientType = a.PatientId != null ? "Registered" : "Guest",
                AppointmentSlotId = a.DoctorAvailabilitySlotId.ToString(),
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime())
            }));

            return response;
        }
    }
}
