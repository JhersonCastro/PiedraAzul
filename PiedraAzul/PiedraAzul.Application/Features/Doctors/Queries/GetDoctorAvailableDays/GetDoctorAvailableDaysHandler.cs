using Mediator;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;

public sealed class GetDoctorAvailableDaysHandler
    : IRequestHandler<GetDoctorAvailableDaysQuery, IReadOnlyList<DateOnly>>
{
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public GetDoctorAvailableDaysHandler(
        IDoctorAvailabilitySlotRepository slotRepository,
        IAppointmentRepository appointmentRepository)
    {
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
    }

    public async ValueTask<IReadOnlyList<DateOnly>> Handle(
        GetDoctorAvailableDaysQuery request,
        CancellationToken cancellationToken)
    {
        // Query 1: All recurring slots for this doctor (non-deleted)
        var slots = await _slotRepository.ListByDoctorAsync(
            request.DoctorId,
            includeDeleted: false,
            cancellationToken);

        if (slots.Count == 0)
            return [];

        // Query 2: All active appointments for this doctor (no date filter → full list)
        var allAppointments = await _appointmentRepository.ListByDoctorAsync(
            request.DoctorId,
            date: null,
            cancellationToken);

        var endDate = request.StartDate.AddDays(request.NumberOfDays - 1);

        // Keep only Active appointments within the requested range
        var occupiedSlotsByDate = allAppointments
            .Where(a =>
                a.Status == AppointmentStatus.Active &&
                a.Date >= request.StartDate &&
                a.Date <= endDate)
            .GroupBy(a => a.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.DoctorAvailabilitySlotId).ToHashSet());

        // Group slots by DayOfWeek for O(1) lookup
        var slotsByDay = slots
            .GroupBy(s => s.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.ToList());

        var availableDays = new List<DateOnly>();

        for (var i = 0; i < request.NumberOfDays; i++)
        {
            var date = request.StartDate.AddDays(i);

            if (!slotsByDay.TryGetValue(date.DayOfWeek, out var daySlots))
                continue; // No slots configured for this weekday

            occupiedSlotsByDate.TryGetValue(date, out var occupiedSlotIds);

            // Day is available if at least one slot has no appointment
            var hasAvailableSlot = daySlots.Any(s =>
                occupiedSlotIds is null || !occupiedSlotIds.Contains(s.Id));

            if (hasAvailableSlot)
                availableDays.Add(date);
        }

        return availableDays;
    }
}
