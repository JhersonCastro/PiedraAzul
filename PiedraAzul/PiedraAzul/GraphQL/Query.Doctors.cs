using Mediator;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.GraphQL.Types;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    public async Task<DoctorType?> GetDoctorAsync(
        string doctorId,
        [Service] IMediator mediator)
    {
        var doctor = await mediator.Send(new GetDoctorByUserIdQuery(doctorId));
        return doctor is null ? null : DoctorType.FromDto(doctor);
    }

    public async Task<List<DoctorType>> GetDoctorsByTypeAsync(
        DoctorSpecialty doctorType,
        [Service] IMediator mediator)
    {
        var doctors = await mediator.Send(
            new GetDoctorsBySpecialtyQuery((Domain.Entities.Shared.Enums.DoctorType)doctorType));
        return doctors.Select(DoctorType.FromDto).ToList();
    }

    public async Task<List<SlotType>> GetDoctorSlotsAsync(
        string doctorId,
        DateTime date,
        [Service] IMediator mediator)
    {
        var day = DateOnly.FromDateTime(date);
        var slots = await mediator.Send(new GetDoctorDaySlotsQuery(doctorId, day));

        return slots.Select(s => new SlotType
        {
            Id = s.Id.ToString(),
            Start = date.Date.Add(s.StartTime),
            End = date.Date.Add(s.EndTime),
            IsAvailable = s.IsAvailable
        }).ToList();
    }

    public async Task<List<SlotType>> GetAvailableSlotsAsync(
        string doctorId,
        DateTime date,
        [Service] IMediator mediator)
    {
        var result = await mediator.Send(
            new GetDoctorDaySlotsQuery(doctorId, DateOnly.FromDateTime(date)));

        return result.Select(s => new SlotType
        {
            Id = s.Id.ToString(),
            Start = date.Add(s.StartTime),
            End = date.Add(s.EndTime),
            IsAvailable = s.IsAvailable
        }).ToList();
    }

    /// <summary>
    /// Devuelve los días (dentro del rango) que tienen al menos un slot libre.
    /// Realiza solo 2 consultas a la BD en lugar de una por día.
    /// </summary>
    public async Task<List<DateTime>> GetDoctorAvailableDaysAsync(
        string doctorId,
        DateTime startDate,
        int numberOfDays,
        [Service] IMediator mediator)
    {
        var start = DateOnly.FromDateTime(startDate);
        var days = await mediator.Send(
            new GetDoctorAvailableDaysQuery(doctorId, start, numberOfDays));

        // Return as UTC DateTime so the client deserializes correctly
        return days.Select(d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToList();
    }

    public async Task<ScheduleConfigType> GetScheduleConfigByDoctorIdAsync(
        string doctorId,
        [Service] ISystemConfigRepository systemConfigRepository,
        [Service] IDoctorAvailabilitySlotRepository slotRepository)
    {
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new GraphQLException("doctorId es requerido");

        var config = await systemConfigRepository.GetOrCreateAsync();
        var allSlots = await slotRepository.ListByDoctorAsync(doctorId, includeDeleted: true);
        var activeSlots = allSlots.Where(s => !s.IsDeleted).OrderBy(s => s.StartTime).ToList();

        var availability = Enum.GetValues<DayOfWeek>()
            .Select(day =>
            {
                var dayActive = activeSlots.Where(s => s.DayOfWeek == day).ToList();
                var startTs = dayActive.Count > 0 ? dayActive.First().StartTime : TimeSpan.Zero;
                var endTs = dayActive.Count > 0 ? dayActive.Last().EndTime : TimeSpan.Zero;
                return new ScheduleDayType
                {
                    DayOfWeek = day.ToString(),
                    IsEnabled = dayActive.Count > 0,
                    StartTime = startTs.ToString(@"hh\:mm\:ss"),
                    EndTime = endTs.ToString(@"hh\:mm\:ss")
                };
            }).ToList();

        var intervalMinutes = activeSlots.Count > 1
            ? (int)System.Math.Max(1,
                (activeSlots.Skip(1).First().StartTime - activeSlots.First().StartTime).TotalMinutes)
            : 15;

        return new ScheduleConfigType
        {
            DoctorId = doctorId,
            BookingWindowWeeks = config.BookingWindowWeeks,
            IntervalMinutes = intervalMinutes,
            Availability = availability,
            Slots = allSlots.Select(s => new RawSlotType
            {
                Id = s.Id.ToString(),
                DayOfWeek = s.DayOfWeek.ToString(),
                StartTime = s.StartTime.ToString(@"hh\:mm\:ss"),
                EndTime = s.EndTime.ToString(@"hh\:mm\:ss"),
                IsDeleted = s.IsDeleted
            }).ToList()
        };
    }
}
