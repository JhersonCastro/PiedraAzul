using Mediator;
using PiedraAzul.Application.Common.Models.Schedule;

namespace PiedraAzul.Application.Features.ScheduleConfig.Queries.GetScheduleConfig;

public record GetScheduleConfigQuery(string DoctorId) : IRequest<ScheduleConfigDto>;
