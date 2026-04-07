using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator;
using PiedraAzul.Application.Common.Models.Appointments;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments;
using PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAppointment(IMediator mediator)
        : AppointmentService.AppointmentServiceBase
    {
        public override async Task<AppointmentResponse> CreateAppointment(
            CreateAppointmentRequest request,
            ServerCallContext context)
        {
            if (request == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date required"));

            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorId required"));

            if (!Guid.TryParse(request.DoctorAvailabilitySlotId, out var slotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid slotId"));

            var date = DateOnly.FromDateTime(request.Date.ToDateTime());

            string? patientUserId = null;
            string? patientGuestId = null;

            switch (request.PatientCase)
            {
                case CreateAppointmentRequest.PatientOneofCase.PatientUserId:
                    patientUserId = request.PatientUserId;
                    break;

                case CreateAppointmentRequest.PatientOneofCase.Guest:
                    var g = request.Guest;

                    if (string.IsNullOrWhiteSpace(g.Identification))
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "Guest identification required"));

                    patientGuestId = g.Identification;
                    break;

                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Patient required"));
            }

            try
            {
                var appointment = await mediator.Send(
                    new CreateAppointmentCommand(
                        request.DoctorId,
                        slotId,
                        date,
                        patientUserId,
                        patientGuestId
                    )
                );

                return MapFromDomain(appointment);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }

        public override async Task<AppointmentListResponse> GetDoctorAppointments(
            DoctorAppointmentsRequest request,
            ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.DoctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorId required"));

            DateOnly? date = null;

            if (request.Date != null)
                date = DateOnly.FromDateTime(request.Date.ToDateTime());

            var result = await mediator.Send(
                new GetDoctorAppointmentsQuery(request.DoctorId, date)
            );

            var response = new AppointmentListResponse();
            response.Appointments.AddRange(result.Select(MapFromDto));
            return response;
        }

        public override async Task<AppointmentListResponse> GetPatientAppointments(
            PatientAppointmentsRequest request,
            ServerCallContext context)
        {
            string? userId = null;
            string? guestId = null;

            switch (request.PatientCase)
            {
                case PatientAppointmentsRequest.PatientOneofCase.PatientUserId:
                    userId = request.PatientUserId;
                    break;

                case PatientAppointmentsRequest.PatientOneofCase.PatientGuestId:
                    guestId = request.PatientGuestId;
                    break;

                default:
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Patient required"));
            }

            var result = await mediator.Send(
                new GetPatientAppointmentsQuery(userId, guestId)
            );

            var response = new AppointmentListResponse();
            response.Appointments.AddRange(
                result.Select(a => MapFromDto(a))
            ); return response;
        }

        // ✅ Mapper para DTO (Queries)
        private static AppointmentResponse MapFromDto(AppointmentDto a)
        {
            return new AppointmentResponse
            {
                Id = a.Id.ToString(),
                PatientUserId = a.PatientUserId ?? "",
                PatientGuestId = a.PatientGuestId ?? "",
                PatientType = a.PatientType,
                PatientName = a.PatientName,

                AppointmentSlotId = a.SlotId.ToString(),

                Start = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(a.Start, DateTimeKind.Utc)
                ),

                CreatedAt = Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime())
            };
        }

        private static AppointmentResponse MapFromDomain(Domain.Entities.Operations.Appointment a)
        {
            var start = a.Date.ToDateTime(TimeOnly.MinValue);

            return new AppointmentResponse
            {
                Id = a.Id.ToString(),
                PatientUserId = a.PatientUserId ?? "",
                PatientGuestId = a.PatientGuestId ?? "",
                PatientType = a.PatientUserId != null ? "Registered" : "Guest",
                PatientName = "", 

                AppointmentSlotId = a.DoctorAvailabilitySlotId.ToString(),

                Start = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(start, DateTimeKind.Utc)
                ),

                CreatedAt = Timestamp.FromDateTime(a.CreatedAt.ToUniversalTime())
            };
        }
    }
}