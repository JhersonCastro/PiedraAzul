using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PiedraAzul.Data.Models;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices
{
    public class GrpcAppointment(PiedraAzul.ApplicationServices.Services.IAppointmentService appointmentService) 
        : AppointmentService.AppointmentServiceBase
    {
        public override  async Task<AppointmentResponse> CreateAppointment(CreateAppointmentRequest request, ServerCallContext context)
        {
            if (request == null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Request cannot be null"));

            if (!Guid.TryParse(request.DoctorId, out var doctorId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            if (!Guid.TryParse(request.DoctorAvailabilitySlotId, out var doctorAvailabilitySlotId))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor availability slot ID format"));

            // if the patientid is not provided, we can create a new patient record using the provided patient information, but we need almost the patient identification
            Guid? patientId = null;

            if (!string.IsNullOrEmpty(request.PatientId))
            {
                if (!Guid.TryParse(request.PatientId, out var parsedPatientId))
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid patient ID format"));

                patientId = parsedPatientId;
            }
            else if (string.IsNullOrWhiteSpace(request.PatientIdentification))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Either patient ID or patient identification must be provided"));
            }


            if (request.Date == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Date must be provided"));

            //Map the gRPC request to the domain model
            Appointment appointment = new Appointment
            {
                DoctorId = doctorId,
                DoctorAvailabilitySlotId = doctorAvailabilitySlotId,
                Date = request.Date.ToDateTime(),
                PatientId = patientId,
                // if the patient id is not provided, we can use the patient identification to create a new patient record, but if the patient id is provided, we should ignore the patient identification and use the existing patient record
                PatientIdentificationNumber = patientId  == null ?  request.PatientIdentification : null,
                PatientName = request.PatientName,
                PatientPhone = request.PatientPhone,
                PatientExtraInfo = request.PatientExtraInfo,
            };

            try
            {
                var result  = await appointmentService.CreateAppointmentAsync(appointment);
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

        }
    }
}
