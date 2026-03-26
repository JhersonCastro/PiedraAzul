using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PiedraAzul.ApplicationServices.Services;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices;

public class GrpcAvailability(IAppointmentService appointmentService)
    : AvailabilityService.AvailabilityServiceBase
{
    public override async Task<AvailableSlotsResponse> GetAvailableSlots(
    AvailableSlotsRequest request,
    ServerCallContext context)
    {
        var response = new AvailableSlotsResponse();

        if (string.IsNullOrWhiteSpace(request.DoctorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required"));

        var date = request.Date.ToDateTime();

        var slots = await appointmentService
            .GetDoctorDaySlotsAsync(request.DoctorId, date);

        foreach (var slot in slots)
        {
            var start = date.Add(slot.Slot.StartTime);
            var end = date.Add(slot.Slot.EndTime);

            response.Slots.Add(new Slot
            {
                Id = slot.Slot.Id.ToString(),

                Start = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(start, DateTimeKind.Utc)
                ),

                End = Timestamp.FromDateTime(
                    DateTime.SpecifyKind(end, DateTimeKind.Utc)
                ),

                IsAvailable = slot.IsAvailable
            });
        }

        return response;
    }
}