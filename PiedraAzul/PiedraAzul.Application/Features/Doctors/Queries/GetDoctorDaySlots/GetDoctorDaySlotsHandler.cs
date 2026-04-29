using Mediator;
using PiedraAzul.Application.Common.Models.Doctor;
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
            .ListByDoctorAsync(request.DoctorId, cancellationToken);

        var daySlots = slots
            .Where(s => s.Matches(request.Date))
            .ToList();

        // 2. Traer citas ocupadas
        var appointments = await _appointmentRepository
            .ListByDoctorAsync(request.DoctorId, request.Date, cancellationToken);

        var occupied = appointments
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