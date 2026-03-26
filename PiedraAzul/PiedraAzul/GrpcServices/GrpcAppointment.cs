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
                    var newGuest = new PatientGuest
                    {
                        PatientName = request.PatientName,
                        PatientPhone = request.PatientPhone,
                        PatientIdentification = request.PatientIdentification
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

            var created = await appointmentService.CreateAppointmentAsync(
                appointment,
                patientUserId,
                request.PatientIdentification
            );

            return new AppointmentResponse
            {
                Id = created.Id.ToString(),
                PatientId = created.PatientUserId ?? "",
                PatientGuestId = created.PatientGuestId ?? "",
                AppointmentSlotId = created.DoctorAvailabilitySlotId.ToString(),
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp
                    .FromDateTime(created.CreatedAt.ToUniversalTime())
            };
        }

        public override async Task<DoctorAppointmentsSearchResponse> GetDoctorAppointments(
            DoctorAppointmentsRequest request,
            ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            var date = request.Date.ToDateTime().ToUniversalTime();

            var search = await appointmentService.SearchDoctorAppointmentsAsync(
                request.DoctorId,
                date,
                request.PageNumber,
                request.PageSize
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
                PatientType = a.PatientType,
                Specialty = a.Specialty,
                Status = a.Status,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp
                    .FromDateTime(a.CreatedAt.ToUniversalTime())
            }));

            return response;
        }
    }
}