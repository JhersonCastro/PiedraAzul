using Mediator;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;

public sealed class GetDoctorAvailableDaysHandler
    : IRequestHandler<GetDoctorAvailableDaysQuery, IReadOnlyList<DateOnly>>
{
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;

    public GetDoctorAvailableDaysHandler(
        IDoctorAvailabilitySlotRepository slotRepository,
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository)
    {
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
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

        // Enforcement: limitar el rango por la ventana de reserva personalizada del doctor.
        var doctor = await _doctorRepository.GetByIdAsync(request.DoctorId, cancellationToken);
        var maxWeeks = doctor?.BookingWindowWeeks ?? 4;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var maxDate = today.AddDays(maxWeeks * 7);

        // Clamp: no permitir días más allá de la ventana, aunque el cliente pida más.
        var effectiveEnd = request.StartDate.AddDays(request.NumberOfDays - 1);
        if (effectiveEnd > maxDate) effectiveEnd = maxDate;
        if (request.StartDate > maxDate) return [];

        // Query 2: All active appointments for this doctor (no date filter → full list)
        var allAppointments = await _appointmentRepository.ListByDoctorAsync(
            request.DoctorId,
            date: null,
            cancellationToken);

        var occupiedSlotsByDate = allAppointments
            .Where(a =>
                a.Status == AppointmentStatus.Active &&
                a.Date >= request.StartDate &&
                a.Date <= effectiveEnd)
            .GroupBy(a => a.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Select(a => a.DoctorAvailabilitySlotId).ToHashSet());

        var slotsByDay = slots
            .GroupBy(s => s.DayOfWeek)
            .ToDictionary(g => g.Key, g => g.ToList());

        var availableDays = new List<DateOnly>();
        var totalDays = effectiveEnd.DayNumber - request.StartDate.DayNumber + 1;

        for (var i = 0; i < totalDays; i++)
        {
            var date = request.StartDate.AddDays(i);

            if (!slotsByDay.TryGetValue(date.DayOfWeek, out var daySlots))
                continue;

            occupiedSlotsByDate.TryGetValue(date, out var occupiedSlotIds);

            var hasAvailableSlot = daySlots.Any(s =>
                occupiedSlotIds is null || !occupiedSlotIds.Contains(s.Id));

            if (hasAvailableSlot)
                availableDays.Add(date);
        }

        return availableDays;
    }
}
