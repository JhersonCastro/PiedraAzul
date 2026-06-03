using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;

public sealed class GetDoctorDaySlotsHandler
    : IRequestHandler<GetDoctorDaySlotsQuery, IReadOnlyList<DoctorSlotAvailabilityDto>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IDoctorAvailabilitySlotRepository _slotRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    public GetDoctorDaySlotsHandler(
    IDoctorRepository doctorRepository,
    IDoctorAvailabilitySlotRepository slotRepository,
    IAppointmentRepository appointmentRepository)
    {
        _doctorRepository = doctorRepository;
        _slotRepository = slotRepository;
        _appointmentRepository = appointmentRepository;
    }

    public async ValueTask<IReadOnlyList<DoctorSlotAvailabilityDto>> Handle(
    GetDoctorDaySlotsQuery request,
    CancellationToken cancellationToken)
    {
        var doctorExists = await _doctorRepository
            .ExistsAsync(request.DoctorId, cancellationToken);

        if (!doctorExists)
            throw new ArgumentException("Doctor not found", nameof(request.DoctorId));

        // 1. Traer slots (entidades)
        var slots = await _slotRepository
            .ListByDoctorAsync(request.DoctorId, includeDeleted: false, cancellationToken);

        var daySlots = slots
            .Where(s => s.Matches(request.Date))
            .ToList();

        // 2. Traer citas que ocupan el slot.
        // Active    → pendiente, slot tomado.
        // Completed → cita atendida, slot tomado (no se puede reusar).
        // NoShow    → paciente no llegó, slot igualmente ya fue asignado.
        // Cancelled → slot liberado, se puede volver a reservar.
        // Rescheduled → soft-delete, ya no cuenta.
        var appointments = await _appointmentRepository
            .ListByDoctorAsync(request.DoctorId, request.Date, cancellationToken);

        var occupied = appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => a.DoctorAvailabilitySlotId)
            .ToHashSet();

        // 3. MAPEO A DTO (aquí está la clave)
        return daySlots
            .Select(slot => new DoctorSlotAvailabilityDto(
                slot.Id,
                slot.StartTime,
                slot.EndTime,
                !occupied.Contains(slot.Id)
            ))
            .ToList();
    }
}