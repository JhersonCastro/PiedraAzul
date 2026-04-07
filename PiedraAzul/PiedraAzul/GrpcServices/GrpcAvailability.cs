using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Mediator;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using Shared.Grpc;

namespace PiedraAzul.GrpcServices;

public class GrpcAvailability(IMediator mediator)
    : AvailabilityService.AvailabilityServiceBase
{
    public override async Task<AvailableSlotsResponse> GetAvailableSlots(
        AvailableSlotsRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DoctorId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "DoctorUserId is required"));

        var date = request.Date.ToDateTime();

        var result = await mediator.Send(
            new GetDoctorDaySlotsQuery(request.DoctorId, DateOnly.FromDateTime(date))
        );

        var response = new AvailableSlotsResponse();

        foreach (var slot in result)
        {
            var start = date.Add(slot.StartTime);
            var end = date.Add(slot.EndTime);

            response.Slots.Add(new Slot
            {
                Id = slot.Id.ToString(),
                Start = Timestamp.FromDateTime(DateTime.SpecifyKind(start, DateTimeKind.Utc)),
                End = Timestamp.FromDateTime(DateTime.SpecifyKind(end, DateTimeKind.Utc)),
                IsAvailable = slot.IsAvailable
            });
        }

        return response;
    }
}