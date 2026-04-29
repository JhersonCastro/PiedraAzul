using Mediator;
using PiedraAzul.Application.Common.Models.Schedule;

namespace PiedraAzul.Application.Features.ScheduleConfig.Commands.SaveScheduleConfig;

public record SaveScheduleConfigCommand(
    string DoctorId,
    int BookingWindowWeeks,
    int IntervalMinutes,
    IReadOnlyList<ScheduleDayDto> Availability) : IRequest<bool>;
