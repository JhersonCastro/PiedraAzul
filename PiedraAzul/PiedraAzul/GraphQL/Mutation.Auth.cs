using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Auth;
using PiedraAzul.Contracts.Validation;
using PiedraAzul.Application.Features.Auth.Commands.Login;
using PiedraAzul.Application.Features.Auth.Commands.MFA;
using PiedraAzul.Application.Features.Auth.Commands.PasswordReset;
using PiedraAzul.Application.Features.Auth.Commands.Register;
using PiedraAzul.Application.Features.Users.Commands.CreateProfileForRole;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Services;
using System.Security.Cryptography;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    public async Task<LoginResultType> LoginAsync(
        LoginInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] SignInManager<ApplicationUser> signInManager,
        [Service] IMFAService mfaService,
        [Service] IMFATokenService mfaTokenService,
        [Service] IMemoryCache cache,
        [Service] ILoginTokenService loginTokenService,
        [Service] IAuditService audit,
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

        // No llamamos SignInAsync aquí: el browser aplicará la cookie vía /auth/apply-session
        var loginToken = loginTokenService.CreateToken(user.Id);

        logger.LogInformation("Successful login for user: {UserId} ({Email})", user.Id, user.Email);

        await audit.LogAsync(
            entityType: "ApplicationUser",
            entityId: user.Id,
            action: "Login",
            data: new { user.Email, roles = result.Roles },
            subjectIdentification: user.IdentificationNumber,
            subjectName: user.Name,
            subjectPhone: user.PhoneNumber);

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
            },
            LoginToken = loginToken
        };
    }

    public async Task<LoginResultType> RegisterAsync(
        RegisterInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] ILoginTokenService loginTokenService,
        [Service] ILogger<Mutation> logger)
    {
        logger.LogInformation("Registration attempt for email: {Email}", input.Email);

        // ── Validación colombiana (servidor) ──────────────────────────────────
        var normalizedPhone = ColombianValidation.NormalizeMobile(input.Phone);
        if (normalizedPhone is null)
            throw new GraphQLException("Ingresa un celular colombiano válido (10 dígitos, empieza por 3).");

        if (!ColombianValidation.IsValidDocumentNumber(input.DocumentType, input.IdentificationNumber))
            throw new GraphQLException(ColombianValidation.DocumentNumberError(input.DocumentType));

        if (!ColombianValidation.IsAtLeastAge(input.BirthDate, ColombianValidation.MinRegistrationAge))
            throw new GraphQLException($"Debes tener al menos {ColombianValidation.MinRegistrationAge} años para registrarte.");

        var result = await mediator.Send(new RegisterCommand(
            new RegisterUserDto(
                input.Email,
                input.Name,
                normalizedPhone,
                input.IdentificationNumber.Trim(),
                (PiedraAzul.Domain.Entities.Shared.Enums.DocumentType)(int)input.DocumentType,
                (PiedraAzul.Domain.Entities.Shared.Enums.GenderType)(int)input.Gender,
                input.BirthDate),
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

        var loginToken = loginTokenService.CreateToken(user.Id);

        logger.LogInformation("Successful registration for user: {UserId} ({Email})", user.Id, user.Email);

        return new LoginResultType
        {
            User = new UserType
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl,
                Roles = input.Roles,
                EmailConfirmed = user.EmailConfirmed
            },
            LoginToken = loginToken
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

    public async Task<LoginResultType> VerifyMFALoginAsync(
        VerifyMFALoginInput input,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] ILoginTokenService loginTokenService,
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

        var loginToken = loginTokenService.CreateToken(user.Id);

        logger.LogInformation("MFA verification successful for user: {UserId}", user.Id);

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
            },
            LoginToken = loginToken
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
        cache.Set(attemptsKey, attempts, TimeSpan.FromHours(1));

        logger.LogInformation($"[MFA] Código reenviado para usuario {userId}. Intento {attempts}/3");

        return true;
    }

    public async Task<LoginResultType> VerifyBackupCodeLoginAsync(
        VerifyBackupCodeLoginInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IMFAService mfaService,
        [Service] IMFATokenService mfaTokenService,
        [Service] ILoginTokenService loginTokenService,
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

        var loginToken = loginTokenService.CreateToken(user.Id);
        logger.LogInformation("Backup code login successful for user: {UserId}", userId);

        var roles = await userManager.GetRolesAsync(user);
        return new LoginResultType
        {
            User = new UserType
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email ?? "",
                AvatarUrl = user.AvatarUrl,
                Roles = roles.ToList(),
                EmailConfirmed = user.EmailConfirmed
            },
            LoginToken = loginToken
        };
    }
}
