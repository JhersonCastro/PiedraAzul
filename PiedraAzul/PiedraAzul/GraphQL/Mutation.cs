using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Auth.Commands.Login;
using PiedraAzul.Application.Features.Auth.Commands.PasswordReset;
using PiedraAzul.Application.Features.Auth.Commands.Register;
using PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public class Mutation
{
    public async Task<UserType> LoginAsync(
        LoginInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] ILogger<Mutation> logger)
    {
        // Check for account lockout before attempting login
        var potentialUser = await userManager.FindByEmailAsync(input.Email)
            ?? await userManager.FindByNameAsync(input.Email);

        if (potentialUser is not null && await userManager.IsLockedOutAsync(potentialUser))
        {
            logger.LogWarning("Login attempt on locked account: {Email}", input.Email);
            throw new GraphQLException("Tu cuenta ha sido bloqueada por demasiados intentos fallidos. Intenta de nuevo en 15 minutos.");
        }

        var result = await mediator.Send(new LoginCommand(input.Email, input.Password));

        if (result.User is null)
        {
            logger.LogWarning("Failed login attempt for email: {Email}", input.Email);
            throw new GraphQLException("Credenciales incorrectas");
        }

        var user = await userManager.FindByIdAsync(result.User.Id)
            ?? throw new GraphQLException("Usuario no encontrado");

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("Successful login for user: {UserId} ({Email})", user.Id, user.Email);

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl,
            Roles = result.Roles
        };
    }

    public async Task<UserType> RegisterAsync(
        RegisterInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogInformation("Registration attempt for email: {Email}", input.Email);

        var result = await mediator.Send(new RegisterCommand(
            new RegisterUserDto(input.Email, input.Name, input.Phone, input.IdentificationNumber),
            input.Password,
            input.Roles
        ));

        if (result.User is null)
        {
            logger.LogWarning("Registration failed for email: {Email}", input.Email);
            throw new GraphQLException("No se pudo registrar");
        }

        foreach (var role in input.Roles)
            await mediator.Send(new CreateProfileForRoleCommand(result.User.Id, role));

        var user = await userManager.FindByIdAsync(result.User.Id)
            ?? throw new GraphQLException("Usuario no encontrado");

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("Successful registration for user: {UserId} ({Email})", user.Id, user.Email);

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl,
            Roles = input.Roles
        };
    }

    public async Task<bool> LogoutAsync(
        [Service] SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return true;
    }

    public async Task<bool> RequestPasswordResetAsync(
        RequestPasswordResetInput input,
        [Service] IMediator mediator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogInformation("Password reset requested for email: {Email}", input.Email);

        var result = await mediator.Send(new RequestPasswordResetCommand(input.Email));

        if (!result)
        {
            logger.LogWarning("Password reset request failed for email: {Email}", input.Email);
            throw new GraphQLException("No se pudo procesar la solicitud de restablecimiento de contraseña");
        }

        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordInput input,
        [Service] IMediator mediator,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogInformation("Password reset attempt for email: {Email}", input.Email);

        var result = await mediator.Send(new ResetPasswordCommand(input.Email, input.Token, input.NewPassword));

        if (!result)
        {
            logger.LogWarning("Password reset failed for email: {Email}", input.Email);
            throw new GraphQLException("No se pudo restablecer la contraseña. El enlace puede haber expirado.");
        }

        logger.LogInformation("Password successfully reset for email: {Email}", input.Email);
        return true;
    }

    [Authorize]
    public async Task<bool> EnableMFAAsync(
        EnableMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IEmailService emailService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await mfaService.EnableMFAAsync(userId, input.Method);
        if (!result)
        {
            logger.LogWarning("Failed to enable MFA for user: {UserId}", userId);
            throw new GraphQLException("No se pudo activar la autenticación de dos factores");
        }

        logger.LogInformation("MFA enabled for user: {UserId} with method: {Method}", userId, input.Method);
        return true;
    }

    [Authorize]
    public async Task<bool> DisableMFAAsync(
        DisableMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        if (!input.Confirm)
            throw new GraphQLException("Debe confirmar para deshabilitar la autenticación de dos factores");

        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await mfaService.DisableMFAAsync(userId);
        if (!result)
        {
            logger.LogWarning("Failed to disable MFA for user: {UserId}", userId);
            throw new GraphQLException("No se pudo deshabilitar la autenticación de dos factores");
        }

        logger.LogInformation("MFA disabled for user: {UserId}", userId);
        return true;
    }

    [Authorize]
    public async Task<bool> InitiateMFAVerificationAsync(
        [Service] IMFAService mfaService,
        [Service] IEmailService emailService,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isMFAEnabled = await mfaService.IsEnabledAsync(userId);
        if (!isMFAEnabled)
            throw new GraphQLException("La autenticación de dos factores no está habilitada");

        var otp = await mfaService.GenerateOTPAsync(userId);
        var user = await userManager.FindByIdAsync(userId);

        if (user?.Email is null)
        {
            logger.LogWarning("Email not found for user: {UserId}", userId);
            throw new GraphQLException("No se pudo enviar el código de verificación");
        }

        var emailSent = await emailService.SendMFAEmailAsync(user.Email, user.Name ?? user.Email, otp, 10);
        if (!emailSent)
        {
            logger.LogWarning("Failed to send MFA email to user: {UserId}", userId);
            throw new GraphQLException("No se pudo enviar el código de verificación");
        }

        logger.LogInformation("MFA verification initiated for user: {UserId}", userId);
        return true;
    }

    public async Task<bool> VerifyMFAAsync(
        VerifyMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("No autenticado");

        var isValid = await mfaService.VerifyOTPAsync(userId, input.OTP);
        if (!isValid)
        {
            logger.LogWarning("Invalid MFA verification attempt for user: {UserId}", userId);
            throw new GraphQLException("Código de verificación inválido");
        }

        logger.LogInformation("MFA verification successful for user: {UserId}", userId);
        return true;
    }

    public async Task<AppointmentType> CreateAppointmentAsync(
        CreateAppointmentInput input,
        [Service] IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId requerido");

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
                ExtraInfo = input.Guest.ExtraInfo
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

    [Authorize]
    public async Task<string> BeginPasskeyRegistrationAsync(
        BeginPasskeyRegistrationInput input,
        [Service] IPasskeyService passkeys,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        // Admin puede registrar passkeys para cualquiera, o el usuario dueño
        if (userId != input.UserId && !isAdmin)
            throw new GraphQLException("No tienes permiso para registrar una passkey en otra cuenta");

        return await passkeys.BeginRegistrationAsync(input.UserId, input.Email, input.DisplayName);
    }

    [Authorize]
    public async Task<bool> CompletePasskeyRegistrationAsync(
        CompletePasskeyRegistrationInput input,
        [Service] IPasskeyService passkeys,
        [Service] ILogger<Mutation> logger,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var isAdmin = httpContextAccessor.HttpContext!.User.IsInRole("Admin");

        // Admin puede completar passkeys para cualquiera, o el usuario dueño
        if (userId != input.UserId && !isAdmin)
            throw new GraphQLException("No tienes permiso para completar una passkey en otra cuenta");

        try
        {
            var result = await passkeys.CompleteRegistrationAsync(
                input.UserId, input.AttestationResponse, input.FriendlyName);

            logger.LogInformation("Passkey registered successfully for user: {UserId} (Name: {FriendlyName})",
                input.UserId, input.FriendlyName);

            return result;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Passkey registration failed for user: {UserId} - {Error}",
                input.UserId, ex.Message);
            throw new GraphQLException(ex.Message);
        }
    }

    public async Task<string> BeginPasskeyAssertionAsync(
        [Service] IPasskeyService passkeys)
    {
        return await passkeys.BeginAssertionAsync();
    }

    public async Task<UserType> CompletePasskeyAssertionAsync(
        CompletePasskeyAssertionInput input,
        [Service] IPasskeyService passkeys,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager)
    {
        try
        {
            var (userId, roles) = await passkeys.CompleteAssertionAsync(input.AssertionResponse);

            var user = await userManager.FindByIdAsync(userId)
                ?? throw new GraphQLException("Usuario no encontrado");

            await signInManager.SignInAsync(user, isPersistent: true);

            return new UserType
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl,
                Roles = roles
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    [Authorize]
    public async Task<bool> DeletePasskeyAsync(
        string passkeyId,
        [Service] IPasskeyService passkeys,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        if (!Guid.TryParse(passkeyId, out var id))
        {
            logger.LogWarning("Invalid passkey ID format attempted for user: {UserId}", userId);
            throw new GraphQLException("ID de passkey inválido");
        }

        var result = await passkeys.DeletePasskeyAsync(userId, id);

        if (result)
            logger.LogInformation("Passkey deleted for user: {UserId} (PasskeyId: {PasskeyId})", userId, passkeyId);

        return result;
    }

    [Authorize]
    public async Task<UserType> UpdateProfileAsync(
        UpdateProfileInput input,
        [Service] IIdentityService identity,
        [Service] IHttpContextAccessor httpContextAccessor)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var user = await identity.UpdateProfileAsync(userId, input.Name, input.AvatarUrl)
            ?? throw new GraphQLException("No se pudo actualizar el perfil");

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Roles = []
        };
    }
}
