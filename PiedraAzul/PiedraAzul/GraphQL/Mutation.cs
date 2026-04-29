using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using PiedraAzul.Application.Common.Interfaces;
using static PiedraAzul.Application.Common.Interfaces.OtpChannel;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Application.Common.Models.Patients;
using PiedraAzul.Application.Features.Appointments.CreateAppointment;
using PiedraAzul.Application.Features.Auth.Commands.Login;
using PiedraAzul.Application.Features.Auth.Commands.MFA;
using PiedraAzul.Application.Features.Auth.Commands.PasswordReset;
using PiedraAzul.Application.Features.Auth.Commands.Register;
using PiedraAzul.Application.Features.ScheduleConfig.Commands.SaveScheduleConfig;
using PiedraAzul.Application.Features.Account.Commands.RequestEmailChange;
using PiedraAzul.Application.Features.Account.Commands.ConfirmEmailChange;
using PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public class Mutation
{
    public async Task<LoginResultType> LoginAsync(
        LoginInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] IMFAService mfaService,
        [Service] IMFATokenService mfaTokenService,
        [Service] IMemoryCache cache,
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

        // Check if MFA is enabled
        var isMFAEnabled = await mfaService.IsEnabledAsync(result.User.Id);

        if (isMFAEnabled)
        {
            var mfaMethod = await mfaService.GetMFAMethodAsync(result.User.Id);
            var mfaToken = mfaTokenService.GenerateMFAToken(result.User.Id);
            var hasEmail = !string.IsNullOrEmpty(user.Email);

            // For Email MFA, generate and send OTP
            if (mfaMethod == "Email" && hasEmail)
            {
                var otp = await mfaService.GenerateOTPAsync(result.User.Id);
                await signInManager.SignOutAsync(); // Ensure no partial login

                // Guardar en cache para que ResendMFACode pueda acceder
                // mfaToken -> userId y mfaToken -> otp
                cache.Set($"mfa:{mfaToken}", otp, TimeSpan.FromMinutes(10));
                cache.Set($"mfa_user:{mfaToken}", result.User.Id, TimeSpan.FromMinutes(10));

                await mfaService.SendOTPEmailAsync(result.User.Id, user.Email!);
            }

            logger.LogInformation("MFA required for user: {UserId}", result.User.Id);

            return new LoginResultType
            {
                MFARequired = new MFARequiredType
                {
                    MFAToken = mfaToken,
                    MFAMethod = mfaMethod,
                    HasEmail = hasEmail
                }
            };
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("Successful login for user: {UserId} ({Email})", user.Id, user.Email);

        return new LoginResultType
        {
            User = new UserType
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl,
                Roles = result.Roles,
                EmailConfirmed = user.EmailConfirmed
            }
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
            logger.LogWarning("Registration failed for email: {Email}. Error: {Error}", input.Email, result.Error);
            throw new GraphQLException(result.Error ?? "No se pudo registrar");
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
            Roles = input.Roles,
            EmailConfirmed = user.EmailConfirmed
        };
    }

    public async Task<bool> LogoutAsync(
        [Service] SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return true;
    }


    public async Task<bool> SaveScheduleConfigAsync(
        ScheduleConfigInput input,
        [Service] IMediator mediator)
    {
        if (string.IsNullOrWhiteSpace(input.DoctorId))
            throw new GraphQLException("DoctorId requerido");

        var result = await mediator.Send(new SaveScheduleConfigCommand(
            input.DoctorId,
            input.BookingWindowWeeks,
            input.IntervalMinutes,
            input.Availability
                .Select(day => new Application.Common.Models.Schedule.ScheduleDayDto(
                    day.DayOfWeek,
                    day.IsEnabled,
                    day.StartTime,
                    day.EndTime))
                .ToList()));

        return result;
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

    public async Task<UserType> VerifyMFALoginAsync(
        VerifyMFALoginInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] ILogger<Mutation> logger)
    {
        var result = await mediator.Send(new VerifyMFALoginCommand(input.MFAToken, input.OTP));

        if (result.User is null)
        {
            logger.LogWarning("MFA verification failed with invalid token or OTP");
            throw new GraphQLException("Código de verificación inválido");
        }

        var user = await userManager.FindByIdAsync(result.User.Id)
            ?? throw new GraphQLException("Usuario no encontrado");

        await signInManager.SignInAsync(user, isPersistent: true);

        logger.LogInformation("MFA verification successful for user: {UserId}", user.Id);

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl,
            Roles = result.Roles,
            EmailConfirmed = user.EmailConfirmed
        };
    }

    [Authorize]
    public async Task<List<string>> EnableMFAAsync(
        EnableMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var backupCodes = await mfaService.EnableMFAAsync(userId, input.Method);
        if (backupCodes.Count == 0)
        {
            logger.LogWarning("Failed to enable MFA for user: {UserId}", userId);
            throw new GraphQLException("No se pudo activar la autenticación de dos factores");
        }

        logger.LogInformation("MFA enabled for user: {UserId} with method: {Method}", userId, input.Method);
        return backupCodes;
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

        var result = await mfaService.DisableMFAAsync(userId, input.Method);
        if (!result)
        {
            logger.LogWarning("Failed to disable MFA for user: {UserId}", userId);
            throw new GraphQLException("No se pudo deshabilitar la autenticación de dos factores");
        }

        logger.LogInformation("MFA disabled for user: {UserId} with method: {Method}", userId, input.Method);
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

    /// <summary>
    /// Envía un OTP al huésped por WhatsApp o Email.
    /// Devuelve un sessionToken para verificar el código después.
    /// </summary>
    public async Task<string> SendGuestOtpAsync(
        string phone,
        string? email,
        string channel,
        [Service] IGuestOtpService guestOtp)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new GraphQLException("El teléfono es requerido.");

        var otpChannel = channel.ToLowerInvariant() switch
        {
            "whatsapp" => OtpChannel.WhatsApp,
            "email"    => OtpChannel.Email,
            _          => throw new GraphQLException("Canal inválido. Usa 'whatsapp' o 'email'.")
        };

        if (otpChannel == OtpChannel.Email && string.IsNullOrWhiteSpace(email))
            throw new GraphQLException("El email es requerido para el canal Email.");

        try
        {
            return await guestOtp.SendAsync(phone, email, otpChannel);
        }
        catch (ArgumentException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    /// <summary>
    /// Verifica el código OTP del huésped.
    /// Retorna true si es válido, false si el código es incorrecto.
    /// Lanza excepción si expiró o se superaron los intentos.
    /// </summary>
    public async Task<bool> VerifyGuestOtpAsync(
        string sessionToken,
        string code,
        [Service] IGuestOtpService guestOtp)
    {
        if (string.IsNullOrWhiteSpace(sessionToken) || string.IsNullOrWhiteSpace(code))
            throw new GraphQLException("sessionToken y code son requeridos.");

        try
        {
            return await guestOtp.VerifyAsync(sessionToken, code);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
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
                Roles = roles,
                EmailConfirmed = user.EmailConfirmed
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
            Roles = [],
            EmailConfirmed = user.EmailConfirmed
        };
    }

    [Authorize]
    public async Task<string> BeginTOTPSetupAsync(
        BeginTOTPSetupInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var qrCode = await mediator.Send(new BeginTOTPSetupCommand(userId, input.Email));

        logger.LogInformation("TOTP setup initiated for user: {UserId}", userId);
        return qrCode;
    }

    [Authorize]
    public async Task<bool> ConfirmTOTPSetupAsync(
        ConfirmTOTPSetupInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await mediator.Send(new ConfirmTOTPSetupCommand(userId, input.TOTP));

        if (!result)
        {
            logger.LogWarning("TOTP setup confirmation failed for user: {UserId}", userId);
            throw new GraphQLException("Código TOTP inválido. Intenta nuevamente.");
        }

        logger.LogInformation("TOTP setup confirmed for user: {UserId}", userId);
        return true;
    }

    [Authorize]
    public async Task<List<string>> GenerateBackupCodesAsync(
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var codes = await mediator.Send(new GenerateBackupCodesCommand(userId));

        logger.LogInformation("Backup codes generated for user: {UserId}", userId);
        return codes;
    }

    [Authorize]
    public async Task<bool> VerifyBackupCodeAsync(
        VerifyMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("No autenticado");

        var isValid = await mfaService.VerifyBackupCodeAsync(userId, input.OTP);
        if (!isValid)
        {
            logger.LogWarning("Invalid backup code verification attempt for user: {UserId}", userId);
            throw new GraphQLException("Código de recuperación inválido");
        }

        logger.LogInformation("Backup code verified for user: {UserId}", userId);
        return true;
    }

    [Authorize]
    public async Task<bool> VerifyTOTPAsync(
        VerifyMFAInput input,
        [Service] IMFAService mfaService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new GraphQLException("No autenticado");

        var isValid = await mfaService.VerifyTOTPAsync(userId, input.OTP);
        if (!isValid)
        {
            logger.LogWarning("Invalid TOTP verification attempt for user: {UserId}", userId);
            throw new GraphQLException("Código TOTP inválido");
        }

        logger.LogInformation("TOTP verification successful for user: {UserId}", userId);
        return true;
    }

    [Authorize]
    public async Task<bool> RequestEmailChangeAsync(
        RequestEmailChangeInput input,
        [Service] IMediator mediator,
        [Service] IIdentityService identityService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var (success, error) = await identityService.RequestEmailChangeAsync(userId, input.NewEmail);

        if (!success)
        {
            logger.LogWarning("Email change request failed for user: {UserId}. Error: {Error}", userId, error);
            throw new GraphQLException(error ?? "No se pudo procesar la solicitud de cambio de correo");
        }

        var result = await mediator.Send(new RequestEmailChangeCommand(userId, input.NewEmail));

        if (!result)
        {
            logger.LogWarning("Email sending failed for user: {UserId}", userId);
            throw new GraphQLException("Error al enviar el código de verificación");
        }

        logger.LogInformation("Email change requested for user: {UserId} to new email: {NewEmail}", userId, input.NewEmail);
        return true;
    }

    [Authorize]
    public async Task<bool> ConfirmEmailChangeAsync(
        ConfirmEmailChangeInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await mediator.Send(new ConfirmEmailChangeCommand(userId, input.NewEmail, input.Code));

        if (!result)
        {
            logger.LogWarning("Email change confirmation failed for user: {UserId}", userId);
            throw new GraphQLException("No se pudo confirmar el cambio de correo. Verifica el código e intenta de nuevo.");
        }

        logger.LogInformation("Email successfully changed for user: {UserId} to: {NewEmail}", userId, input.NewEmail);
        return true;
    }

    [Authorize]
    public async Task<bool> SendEmailVerificationCodeAsync(
        [Service] IIdentityService identityService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var user = await identityService.GetById(userId);
        if (user is null || string.IsNullOrEmpty(user.Email))
            throw new GraphQLException("Usuario o email no encontrado");

        var emailResult = await identityService.SendEmailVerificationCodeAsync(userId, user.Email);
        if (!emailResult)
        {
            logger.LogWarning("Failed to send email verification code for user: {UserId}", userId);
            throw new GraphQLException("No se pudo enviar el código de verificación");
        }

        logger.LogInformation("Email verification code sent for user: {UserId}", userId);
        return true;
    }

    [Authorize]
    public async Task<bool> VerifyEmailCodeAsync(
        string code,
        [Service] IIdentityService identityService,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var result = await identityService.VerifyEmailCodeAsync(userId, code);
        if (!result)
        {
            logger.LogWarning("Invalid email verification code for user: {UserId}", userId);
            throw new GraphQLException("Código de verificación inválido");
        }

        logger.LogInformation("Email verified for user: {UserId}", userId);
        return true;
    }

    public async Task<UserType> VerifyBackupCodeLoginAsync(
        VerifyBackupCodeLoginInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] IMFAService mfaService,
        [Service] IMFATokenService mfaTokenService,
        [Service] ILogger<Mutation> logger)
    {
        // Validar el mfaToken y obtener el userId
        var userId = mfaTokenService.ValidateMFAToken(input.MFAToken)
            ?? throw new GraphQLException("Token expirado. Inicia sesión nuevamente.");

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new GraphQLException("Usuario no encontrado");

        // Verificar el backup code
        var isValid = await mfaService.VerifyBackupCodeAsync(userId, input.BackupCode);

        if (!isValid)
        {
            logger.LogWarning("Backup code login failed for user: {UserId}", userId);
            throw new GraphQLException("Código de recuperación inválido o ya utilizado");
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        logger.LogInformation("Backup code login successful for user: {UserId}", userId);

        var roles = await userManager.GetRolesAsync(user);
        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            AvatarUrl = user.AvatarUrl,
            Roles = roles.ToList(),
            EmailConfirmed = user.EmailConfirmed
        };
    }

    public async Task<bool> ResendMFACodeAsync(
        string mfaToken,
        [Service] IMFAService mfaService,
        [Service] IEmailService emailService,
        [Service] IMemoryCache cache,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] ILogger<Mutation> logger)
    {
        // RATE LIMITING: Máx 3 reintentos por mfaToken
        var attemptsKey = $"resend_attempts:{mfaToken}";
        var attempts = cache.TryGetValue(attemptsKey, out int currentAttempts) ? currentAttempts : 0;

        if (attempts >= 3)
        {
            logger.LogWarning($"[MFA] Rate limit: Excedidos 3 reintentos para token {mfaToken}");
            throw new GraphQLException("Demasiados intentos de reenvío. Intenta de nuevo más tarde.");
        }

        // Verificar que el mfaToken es válido (existe en cache)
        // El mfaToken vincula al usuario, buscamos en cache
        var tokenUserPrefix = $"mfa_user:{mfaToken}";
        var tokenExists = cache.TryGetValue(tokenUserPrefix, out string? userId);

        if (!tokenExists || string.IsNullOrEmpty(userId))
        {
            logger.LogWarning($"[MFA] Token expirado o inválido: {mfaToken}");
            throw new GraphQLException("Tu sesión de verificación ha expirado. Por favor inicia sesión nuevamente.");
        }

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new GraphQLException("Usuario no encontrado");

        // Generar nuevo código OTP
        var otp = await mfaService.GenerateOTPAsync(userId);

        // Guardar en cache con el MISMO token (sobrescribe el anterior, mantiene 10 minutos)
        cache.Set($"mfa:{mfaToken}", otp, TimeSpan.FromMinutes(10));
        cache.Set(tokenUserPrefix, userId, TimeSpan.FromMinutes(10));

        // Enviar por email
        var emailSent = await emailService.SendMFAEmailAsync(user.Email, user.Name ?? user.Email, otp, 10);
        if (!emailSent)
        {
            logger.LogWarning($"[MFA] Fallo al enviar email a {user.Email}");
            throw new GraphQLException("No se pudo enviar el código. Intenta de nuevo.");
        }

        // Incrementar contador de reintentos
        attempts++;
        cache.Set(attemptsKey, attempts, TimeSpan.FromHours(1)); // Resetea después de 1 hora

        logger.LogInformation($"[MFA] Código reenviado para usuario {userId}. Intento {attempts}/3");

        return true;
    }
}
