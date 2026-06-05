using HotChocolate.Authorization;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

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
        bool onlyAvailable,
        [Service] IMediator mediator,
        [Service] ICacheService cache)
    {
        var doctors = await cache.GetOrCreateAsync(
            CacheKeys.DoctorsBySpecialty((int)doctorType, onlyAvailable),
            _ => mediator.Send(
                new GetDoctorsBySpecialtyQuery((Domain.Entities.Shared.Enums.DoctorType)doctorType, onlyAvailable)).AsTask(),
            TimeSpan.FromMinutes(10),
            new[] { CacheKeys.TagSpecialty((int)doctorType) });

        return doctors.Select(DoctorType.FromDto).ToList();
    }

    public async Task<List<SlotType>> GetDoctorSlotsAsync(
        string doctorId,
        DateTime date,
        [Service] IMediator mediator,
        [Service] ICacheService cache)
    {
        var day = DateOnly.FromDateTime(date);
        var slots = await cache.GetOrCreateAsync(
            CacheKeys.DoctorDaySlots(doctorId, day),
            _ => mediator.Send(new GetDoctorDaySlotsQuery(doctorId, day)).AsTask(),
            TimeSpan.FromSeconds(90),
            new[] { CacheKeys.TagDoctor(doctorId) });

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
        [Service] IMediator mediator,
        [Service] ICacheService cache)
    {
        var day = DateOnly.FromDateTime(date);
        var result = await cache.GetOrCreateAsync(
            CacheKeys.DoctorDaySlots(doctorId, day),
            _ => mediator.Send(new GetDoctorDaySlotsQuery(doctorId, day)).AsTask(),
            TimeSpan.FromSeconds(90),
            new[] { CacheKeys.TagDoctor(doctorId) });

        return result.Select(s => new SlotType
        {
            Id = s.Id.ToString(),
            Start = date.Date.Add(s.StartTime),
            End = date.Date.Add(s.EndTime),
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
        [Service] IMediator mediator,
        [Service] ICacheService cache)
    {
        var start = DateOnly.FromDateTime(startDate);
        var days = await cache.GetOrCreateAsync(
            CacheKeys.DoctorAvailableDays(doctorId, start, numberOfDays),
            _ => mediator.Send(new GetDoctorAvailableDaysQuery(doctorId, start, numberOfDays)).AsTask(),
            TimeSpan.FromMinutes(2),
            new[] { CacheKeys.TagDoctorDays(doctorId), CacheKeys.TagDoctor(doctorId) });

        // Return as UTC DateTime so the client deserializes correctly
        return days.Select(d => d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)).ToList();
    }

    /// <summary>
    /// Devuelve los pacientes únicos que tienen (o tuvieron) citas con este doctor.
    /// Solo el propio doctor o un Admin puede consultarlo.
    /// </summary>
    [Authorize(Roles = new[] { "Doctor", "Admin" })]
    public async Task<List<DoctorPatientType>> GetDoctorPatientsAsync(
        string doctorId,
        [Service] IAppointmentRepository appointmentRepository,
        [Service] IPatientGuestRepository guestRepository,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        if (userId != doctorId && !isAdmin)
            throw new GraphQLException("No tienes permiso para ver los pacientes de este doctor.");

        // Todas las citas del doctor (Active, Completed, NoShow, Cancelled)
        var appointments = await appointmentRepository.ListByDoctorAsync(doctorId);

        var result = new List<DoctorPatientType>();

        // --- Pacientes Registrados ---
        var registeredGroups = appointments
            .Where(a => a.PatientUserId != null)
            .GroupBy(a => a.PatientUserId!)
            .ToList();

        var registeredIds = registeredGroups.Select(g => g.Key).ToList();
        var users = registeredIds.Any()
            ? await userManager.Users
                .Where(u => registeredIds.Contains(u.Id) && !u.IsDeleted)
                .ToListAsync()
            : [];

        var usersDict = users.ToDictionary(u => u.Id);

        foreach (var group in registeredGroups)
        {
            usersDict.TryGetValue(group.Key, out var user);
            var lastVisit = group.Max(a => a.Date);
            result.Add(new DoctorPatientType
            {
                Id = group.Key,
                Name = user?.Name ?? group.Key,
                Identification = user?.IdentificationNumber ?? "",
                Phone = user?.PhoneNumber ?? "",
                Type = PatientTypeEnum.Registered,
                LastVisit = lastVisit.ToDateTime(TimeOnly.MinValue)
            });
        }

        // --- Pacientes Invitados ---
        // Construimos un índice de identificaciones de usuarios registrados para deduplicar:
        // Si un invitado tiene la misma identificación que un registrado, es la misma persona
        // (ocurre cuando alguien con cuenta fue agendado como invitado por error).
        var registeredIdentifications = users
            .Where(u => !string.IsNullOrWhiteSpace(u.IdentificationNumber))
            .Select(u => u.IdentificationNumber!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // También un índice para actualizar la última visita si el invitado tiene fecha más reciente
        var registeredByIdentification = result
            .ToDictionary(r => r.Identification?.Trim() ?? "", StringComparer.OrdinalIgnoreCase);

        var guestGroups = appointments
            .Where(a => a.PatientGuestId != null)
            .GroupBy(a => a.PatientGuestId!)
            .ToList();

        var guestIds = guestGroups.Select(g => g.Key).ToList();
        var guests = guestIds.Any()
            ? await guestRepository.GetByIdsAsync(guestIds, CancellationToken.None)
            : [];
        var guestsDict = guests.ToDictionary(g => g.Id);

        foreach (var group in guestGroups)
        {
            guestsDict.TryGetValue(group.Key, out var guest);
            var lastVisit = group.Max(a => a.Date);
            // La identificación del invitado es su PatientGuestId (que es su número de identificación)
            var identification = guest?.Id ?? group.Key;

            // Si esta identificación ya existe en los registrados → es el mismo paciente, fusionar
            if (registeredIdentifications.Contains(identification))
            {
                // Actualizar la última visita del registrado si la del invitado es más reciente
                if (registeredByIdentification.TryGetValue(identification, out var existingRegistered))
                {
                    var guestLastVisitDt = lastVisit.ToDateTime(TimeOnly.MinValue);
                    if (existingRegistered.LastVisit.HasValue && guestLastVisitDt > existingRegistered.LastVisit.Value)
                        existingRegistered.LastVisit = guestLastVisitDt;
                }
                // No agregar duplicado
                continue;
            }

            result.Add(new DoctorPatientType
            {
                Id             = group.Key,
                Name           = guest?.Name ?? group.Key,
                Identification = identification,
                Phone          = guest?.Phone ?? "",
                Type           = PatientTypeEnum.Guest,
                LastVisit      = lastVisit.ToDateTime(TimeOnly.MinValue)
            });
        }

        return result.OrderBy(p => p.Name).ToList();
    }

    public async Task<ScheduleConfigType> GetScheduleConfigByDoctorIdAsync(
        string doctorId,
        [Service] IDoctorRepository doctorRepository,
        [Service] IDoctorAvailabilitySlotRepository slotRepository)
    {
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new GraphQLException("doctorId es requerido");

        var doctor = await doctorRepository.GetByIdAsync(doctorId);
        var bookingWeeks = doctor?.BookingWindowWeeks ?? 4;
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
            BookingWindowWeeks = bookingWeeks,
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
