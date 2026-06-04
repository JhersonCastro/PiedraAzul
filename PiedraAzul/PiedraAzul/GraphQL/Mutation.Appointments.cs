using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Contracts.Validation;
using PiedraAzul.Application.Features.Appointments.CancelAppointment;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Appointments.RescheduleAppointment;
using PiedraAzul.Application.Features.Appointments.UpdateAppointmentStatus;
using PiedraAzul.Domain.Entities.Operations;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    [Authorize(Roles = new[] { "Doctor", "Admin", "Patient" })]
    public async Task<AppointmentType> CreateAppointmentAsync(
        CreateAppointmentInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId requerido");

        var currentUserId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isDoctor = httpContextAccessor.HttpContext!.User.IsInRole("Doctor");

        if (isDoctor && input.DoctorId != currentUserId)
            throw new GraphQLException("Los doctores solo pueden crear citas para sí mismos");

        if (!Guid.TryParse(input.DoctorAvailabilitySlotId, out var slotId))
            throw new GraphQLException("SlotId inválido");

        var date = DateOnly.FromDateTime(input.Date);

        GuestPatientRequest? patientGuest = null;

        if (input.Guest is not null)
        {
            patientGuest = new GuestPatientRequest
            {
                Identification = input.Guest.Identification,
                Name = input.Guest.Name,
                Phone = input.Guest.Phone,
                ExtraInfo = input.Guest.ExtraInfo,
                Email = input.Guest.Email,
                DocumentType = (PiedraAzul.Domain.Entities.Shared.Enums.DocumentType)(int)input.Guest.DocumentType
            };
        }

        var appointment = await mediator.Send(
            new CreateAppointmentCommand(
                input.DoctorId,
                slotId,
                date,
                input.PatientUserId,
                patientGuest
            )
        );

        return AppointmentType.FromDomain(appointment);
    }

    /// <summary>
    /// Booking público para invitados desde InstantBooking. No requiere autenticación.
    /// Solo acepta citas con datos de paciente invitado (Guest).
    /// </summary>
    public async Task<AppointmentType> BookGuestAppointmentAsync(
        CreateAppointmentInput input,
        [Service] IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId requerido");

        if (input.Guest is null)
            throw new GraphQLException("Los datos del paciente son requeridos para el agendamiento como invitado");

        if (!Guid.TryParse(input.DoctorAvailabilitySlotId, out var slotId))
            throw new GraphQLException("SlotId inválido");

        var date = DateOnly.FromDateTime(input.Date);

        // Validación colombiana (servidor) para el booking público de invitados.
        var normalizedPhone = ColombianValidation.NormalizeMobile(input.Guest.Phone);
        if (normalizedPhone is null)
            throw new GraphQLException("Ingresa un celular colombiano válido (10 dígitos, empieza por 3).");

        if (!ColombianValidation.IsValidDocumentNumber(input.Guest.DocumentType, input.Guest.Identification))
            throw new GraphQLException(ColombianValidation.DocumentNumberError(input.Guest.DocumentType));

        var patientGuest = new GuestPatientRequest
        {
            Identification = input.Guest.Identification.Trim(),
            Name = input.Guest.Name,
            Phone = normalizedPhone,
            ExtraInfo = input.Guest.ExtraInfo,
            Email = input.Guest.Email,
            DocumentType = (PiedraAzul.Domain.Entities.Shared.Enums.DocumentType)(int)input.Guest.DocumentType
        };

        var appointment = await mediator.Send(
            new CreateAppointmentCommand(
                input.DoctorId,
                slotId,
                date,
                null,
                patientGuest
            )
        );

        return AppointmentType.FromDomain(appointment);
    }

    /// <summary>
    /// Cancela una cita activa. Solo el paciente dueño de la cita puede cancelarla.
    /// </summary>
    [Authorize(Roles = new[] { "Patient" })]
    public async Task<bool> CancelAppointmentAsync(
        Guid appointmentId,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        return await mediator.Send(new CancelAppointmentCommand(appointmentId, userId));
    }

    /// <summary>
    /// Reagenda una cita para un paciente autenticado (registrado).
    /// </summary>
    [Authorize(Roles = new[] { "Patient", "Admin", "Doctor" })]
    public async Task<AppointmentType> RescheduleAppointmentAsync(
        Guid appointmentId,
        Guid newSlotId,
        DateTime newDate,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        // El permiso (paciente dueño / doctor de la cita / admin) se deriva del userId en el handler.
        var appointment = await mediator.Send(
            new RescheduleAppointmentCommand(
                appointmentId,
                userId,
                newSlotId,
                DateOnly.FromDateTime(newDate)));

        return AppointmentType.FromDomain(appointment);
    }

    /// <summary>
    /// Permite al doctor marcar una de sus citas como Completada (Completed)
    /// o como Inasistencia (NoShow). Solo el doctor dueño puede hacerlo.
    /// </summary>
    [Authorize(Roles = new[] { "Doctor", "Admin" })]
    public async Task<bool> UpdateAppointmentStatusAsync(
        Guid appointmentId,
        string newStatus,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        if (!Enum.TryParse<AppointmentStatus>(newStatus, ignoreCase: true, out var status))
            throw new GraphQLException($"Estado inválido: {newStatus}. Use 'Completed' o 'NoShow'.");

        return await mediator.Send(new UpdateAppointmentStatusCommand(appointmentId, userId, status));
    }

    /// <summary>
    /// Reagenda una cita para un invitado verificado mediante hash OTP.
    /// No requiere autenticación.
    /// </summary>
    public async Task<AppointmentType> RescheduleAppointmentByHashAsync(
        string verificationHash,
        Guid appointmentId,
        Guid newSlotId,
        DateTime newDate,
        [Service] IMediator mediator)
    {
        var appointment = await mediator.Send(
            new RescheduleAppointmentByHashCommand(
                verificationHash,
                appointmentId,
                newSlotId,
                DateOnly.FromDateTime(newDate)));

        return AppointmentType.FromDomain(appointment);
    }
}
