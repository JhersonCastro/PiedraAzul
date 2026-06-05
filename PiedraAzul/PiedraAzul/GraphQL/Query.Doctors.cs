using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAvailableDays;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorPatients;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Application.Features.Doctors.Queries.GetScheduleConfig;
using PiedraAzul.Contracts.DTOs;
using PiedraAzul.GraphQL.Types;
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

    public async Task<List<SlotDto>> GetDoctorSlotsAsync(
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

        return slots.Select(s => new SlotDto
        {
            Id = s.Id.ToString(),
            Start = date.Date.Add(s.StartTime),
            End = date.Date.Add(s.EndTime),
            IsAvailable = s.IsAvailable
        }).ToList();
    }

    public async Task<List<SlotDto>> GetAvailableSlotsAsync(
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

        return result.Select(s => new SlotDto
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
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        if (userId != doctorId && !isAdmin)
            throw new GraphQLException("No tienes permiso para ver los pacientes de este doctor.");

        var patients = await mediator.Send(new GetDoctorPatientsQuery(doctorId));

        return patients.Select(p => new DoctorPatientType
        {
            Id = p.Id,
            Name = p.Name,
            Identification = p.Identification,
            Phone = p.Phone,
            Type = (PatientTypeEnum)(int)p.Type,
            LastVisit = p.LastVisit
        }).ToList();
    }

    public async Task<ScheduleConfigType> GetScheduleConfigByDoctorIdAsync(
        string doctorId,
        [Service] IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(doctorId))
            throw new GraphQLException("doctorId es requerido");

        var config = await mediator.Send(new GetScheduleConfigByDoctorIdQuery(doctorId));
        return ScheduleConfigType.FromDto(config);
    }
}
