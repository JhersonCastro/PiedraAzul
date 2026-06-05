using HotChocolate;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Repositories;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    public async Task<bool> SaveScheduleConfigAsync(
        ScheduleConfigInput input,
        [Service] IDoctorRepository doctorRepository,
        [Service] IDoctorAvailabilitySlotRepository slotRepository,
        [Service] IUnitOfWork unitOfWork,
        [Service] ICacheService cache)
    {
        if (input is null)
            throw new GraphQLException("La configuración es requerida");

        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId es requerido");

        if (input.BookingWindowWeeks < 1)
            throw new GraphQLException("BookingWindowWeeks debe ser mayor a 0");

        var activeSlots = input.ActiveSlots ?? [];

        foreach (var slot in activeSlots)
        {
            if (!TimeSpan.TryParse(slot.StartTime, out var start) || !TimeSpan.TryParse(slot.EndTime, out var end))
                throw new GraphQLException($"Formato de tiempo inválido para {slot.DayOfWeek}");

            if (start >= end)
                throw new GraphQLException($"Rango inválido para {slot.DayOfWeek}: StartTime debe ser menor que EndTime");
        }

        await unitOfWork.ExecuteAsync(async ct =>
        {
            // Guardar BookingWindowWeeks en el Doctor (por doctor, no global).
            var doctor = await doctorRepository.GetByIdAsync(input.DoctorId, ct)
                ?? throw new GraphQLException($"Doctor '{input.DoctorId}' no encontrado.");
            doctor.SetBookingWindow(input.BookingWindowWeeks);
            await doctorRepository.UpdateAsync(doctor, ct);

            var desired = activeSlots
                .Select(s => {
                    TimeSpan.TryParse(s.StartTime, out var start);
                    TimeSpan.TryParse(s.EndTime, out var end);
                    return (s.DayOfWeek, start, end);
                })
                .ToHashSet();

            var existing = await slotRepository.ListByDoctorAsync(input.DoctorId, includeDeleted: true, ct);
            var handled = new HashSet<(DayOfWeek, TimeSpan, TimeSpan)>();

            foreach (var slot in existing)
            {
                var key = (slot.DayOfWeek, slot.StartTime, slot.EndTime);
                if (desired.Contains(key))
                {
                    handled.Add(key);
                    if (slot.IsDeleted)
                    {
                        slot.Restore();
                        await slotRepository.UpdateAsync(slot, ct);
                    }
                }
                else if (!slot.IsDeleted)
                {
                    slot.SoftDelete();
                    await slotRepository.UpdateAsync(slot, ct);
                }
            }

            foreach (var (day, start, end) in desired.Except(handled))
            {
                await slotRepository.AddAsync(new DoctorAvailabilitySlot(
                    input.DoctorId, day, start, end), ct);
            }

            return true;
        });

        // Cambió el horario del doctor → invalidar todos sus días y slots cacheados.
        cache.RemoveByTag(CacheKeys.TagDoctor(input.DoctorId));

        return true;
    }
}
