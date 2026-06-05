using Mediator;
using PiedraAzul.Application.Common.Models.Schedule;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetScheduleConfig;

public class GetScheduleConfigByDoctorIdHandler
    : IRequestHandler<GetScheduleConfigByDoctorIdQuery, ScheduleConfigDto>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;

    public GetScheduleConfigByDoctorIdHandler(
        IDoctorRepository doctorRepository,
        IDoctorAvailabilitySlotRepository slotRepository)
    {
        _doctorRepository = doctorRepository;
        _slotRepository = slotRepository;
    }

    public async ValueTask<ScheduleConfigDto> Handle(
        GetScheduleConfigByDoctorIdQuery request,
        CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId);
        var bookingWeeks = doctor?.BookingWindowWeeks ?? 4;

        var allSlots = await _slotRepository.ListByDoctorAsync(request.DoctorId, includeDeleted: true);
        var activeSlots = allSlots.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime).ToList();

        var availability = Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var dayActive = activeSlots.Where(s => s.DayOfWeek == day).ToList();
                var startTs = dayActive.Count > 0 ? dayActive.First().StartTime : TimeSpan.Zero;
                var endTs = dayActive.Count > 0 ? dayActive.Last().EndTime : TimeSpan.Zero;
                return new ScheduleDayDto(day, dayActive.Count > 0, startTs, endTs);
            })
            .ToList();

        var intervalMinutes = activeSlots.Count > 1
            ? (int)System.Math.Max(1,
                (activeSlots.Skip(1).First().StartTime - activeSlots.First().StartTime).TotalMinutes)
            : 15;

        var slots = allSlots
            .Select(s => new RawSlotDto(s.Id, s.DayOfWeek, s.StartTime, s.EndTime, s.IsDeleted))
            .ToList();

        return new ScheduleConfigDto(
            request.DoctorId,
            bookingWeeks,
            intervalMinutes,
            availability,
            slots);
    }
}
