using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Http;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Account.Commands.ConfirmEmailChange;
using PiedraAzul.Application.Features.Account.Commands.RequestEmailChange;
using PiedraAzul.Application.Features.Auth.Commands.ChangePassword;
using PiedraAzul.Application.Features.Auth.Commands.MFA;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    [Authorize]
    public async Task<bool> ChangePasswordAsync(
        ChangePasswordInput input,
        [Service] IMediator mediator,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] ILogger<Mutation> logger)
    {
        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var (success, error) = await mediator.Send(new ChangePasswordCommand(userId, input.CurrentPassword, input.NewPassword));

        if (!success)
        {
            logger.LogWarning("Password change failed for user: {UserId}. Error: {Error}", userId, error);
            throw new GraphQLException(error ?? "No se pudo actualizar la contraseña");
        }

        logger.LogInformation("Password successfully changed for user: {UserId}", userId);
        return true;
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
}
