using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorByUserId;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorDaySlots;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorsBySpecialty;
using PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments;
using PiedraAzul.Application.Features.Patients.Queries.SearchPatients;
using PiedraAzul.Application.Features.Users.Queries.GetUserById;
using PiedraAzul.Application.Features.Users.Queries.GetUserRoles;
using PiedraAzul.GraphQL.Types;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public class Query
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

    public async Task<List<AppointmentType>> GetDoctorAppointmentsAsync(
        string doctorId,
        DateTime? date,
        [Service] IMediator mediator)
    {
        DateOnly? dateOnly = date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
        var result = await mediator.Send(new GetDoctorAppointmentsQuery(doctorId, dateOnly));
        return result.Select(AppointmentType.FromDto).ToList();
    }

    public async Task<List<AppointmentType>> GetPatientAppointmentsAsync(
        string? patientUserId,
        string? patientGuestId,
        [Service] IMediator mediator)
    {
        var result = await mediator.Send(new GetPatientAppointmentsQuery(patientUserId, patientGuestId));
        return result.Select(AppointmentType.FromDto).ToList();
    }

    public async Task<List<PatientSearchResultType>> SearchPatientsAsync(
        string query,
        int? limit,
        [Service] IMediator mediator)
    {
        var patients = await mediator.Send(new SearchPatientsQuery(query));
        return patients
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderBy(p => p.Name)
            .Take(limit ?? int.MaxValue)
            .Select(p => new PatientSearchResultType
            {
                Id = p.Id,
                Name = p.Name,
                Identification = p.Id,
                Phone = "",
                Type = p.Type == "Guest" ? PatientTypeEnum.Guest : PatientTypeEnum.Registered
            })
            .ToList();
    }

    public async Task<List<PatientSearchResultType>> SearchAutoCompletePatientsAsync(
        string query,
        [Service] IMediator mediator)
    {
        var patients = await mediator.Send(new SearchPatientsQuery(query));
        return patients.Select(p => new PatientSearchResultType
        {
            Id = p.Id,
            Name = p.Name,
            Identification = p.Id,
            Phone = "",
            Type = p.Type == "Guest" ? PatientTypeEnum.Guest : PatientTypeEnum.Registered
        }).ToList();
    }

    [Authorize]
    public async Task<UserType> GetCurrentUserAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var user = await mediator.Send(new GetUserByIdQuery(userId))
            ?? throw new GraphQLException("Usuario no encontrado");

        var roles = await mediator.Send(new GetUserRolesQuery(userId));

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Roles = roles
        };
    }

    [Authorize]
    public async Task<List<PasskeyType>> GetMyPasskeysAsync(
        [Service] IPasskeyService passkeys,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var list = await passkeys.GetUserPasskeysAsync(userId);

        return list.Select(p => new PasskeyType
        {
            Id = p.Id.ToString(),
            FriendlyName = p.FriendlyName,
            CreatedAt = p.CreatedAt
        }).ToList();
    }
}
