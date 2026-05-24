using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Doctors.Queries.GetDoctorAppointments;
using PiedraAzul.Application.Features.Patients.Queries.GetPatientAppointments;
using PiedraAzul.GraphQL.Types;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    [Authorize]
    public async Task<List<AppointmentType>> GetDoctorAppointmentsAsync(
        string doctorId,
        DateTime? date,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        if (userId != doctorId && !isAdmin)
            throw new GraphQLException("No tienes permiso para ver estas citas");

        DateOnly? dateOnly = date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
        var result = await mediator.Send(new GetDoctorAppointmentsQuery(doctorId, dateOnly));
        var resultDto = result.Select(AppointmentType.FromDto).ToList();
        return resultDto;
    }

    [Authorize]
    public async Task<List<AppointmentType>> GetMyUpcomingAppointmentsAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await mediator.Send(new GetPatientAppointmentsQuery(userId, null));

        var now = DateTime.UtcNow.Date;
        return result
            .Where(a => a.Start >= now)
            .OrderBy(a => a.Start)
            .Select(AppointmentType.FromDto)
            .ToList();
    }

    [Authorize]
    public async Task<List<AppointmentType>> GetMyAppointmentsAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");
        
        var result = await mediator.Send(new GetPatientAppointmentsQuery(userId, null));
        return result
            .OrderByDescending(a => a.Start)
            .Select(AppointmentType.FromDto)
            .ToList();
    }

    [Authorize]
    public async Task<List<AppointmentType>> GetPatientAppointmentsAsync(
        string? patientUserId,
        string? patientGuestId,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        if (!string.IsNullOrEmpty(patientUserId) && userId != patientUserId && !isAdmin)
            throw new GraphQLException("No tienes permiso para ver estas citas");

        var result = await mediator.Send(new GetPatientAppointmentsQuery(patientUserId, patientGuestId));
        return result.Select(AppointmentType.FromDto).ToList();
    }

    /// <summary>
    /// Retorna las citas próximas de un invitado verificado mediante hash OTP.
    /// No requiere autenticación.
    /// </summary>
    public async Task<List<AppointmentType>> GetGuestUpcomingAppointmentsAsync(
        string verificationHash,
        [Service] IMediator mediator,
        [Service] IGuestOtpService guestOtpService)
    {
        var verified = await guestOtpService.IsSessionVerifiedAsync(verificationHash);
        if (!verified)
            throw new GraphQLException("La sesión de verificación no es válida o expiró.");

        var sessionData = await guestOtpService.GetVerifiedDataAsync(verificationHash)
            ?? throw new GraphQLException("No se pudieron obtener los datos de verificación.");

        // Route by session type: registered user → userId, guest → guestId
        var query = sessionData.SessionType == PiedraAzul.Domain.Entities.Profiles.Patients.VerificationSessionType.RegisteredUser
            ? new GetPatientAppointmentsQuery(sessionData.Id, null)
            : new GetPatientAppointmentsQuery(null, sessionData.Id);

        var result = await mediator.Send(query);

        var now = DateTime.UtcNow.Date;
        return result
            .Where(a => a.Start >= now)
            .OrderBy(a => a.Start)
            .Select(AppointmentType.FromDto)
            .ToList();
    }
}
