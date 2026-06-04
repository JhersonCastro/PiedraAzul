using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Claims;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    /// <summary>
    /// Envía un OTP al huésped por WhatsApp, SMS o Email.
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

        var otpChannel = ParseChannel(channel);

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
    /// Verifica el código OTP del huésped (FLUJO 1 - nuevo usuario).
    /// Retorna true si es válido, false si el código es incorrecto.
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

    // ── Pre-verificación para guests existentes y usuarios registrados (FLUJO 2 y FLUJO 3) ──

    /// <summary>Envía OTP usando la sesión hash generada en el lookup.</summary>
    public async Task<bool> SendGuestOtpByHashAsync(
        string hash,
        string channel,
        [Service] IGuestOtpService guestOtp)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new GraphQLException("El hash es requerido.");

        var otpChannel = ParseChannel(channel);

        try
        {
            await guestOtp.SendOtpByHashAsync(hash, otpChannel);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    /// <summary>
    /// Verifica el OTP de la sesión hash y opcionalmente actualiza datos editados por el usuario.
    /// Si es correcto retorna los datos finales del usuario.
    /// </summary>
    public async Task<GuestDataType?> VerifyGuestOtpByHashAsync(
        string hash,
        string code,
        string? name,
        string? phone,
        string? email,
        [Service] IGuestOtpService guestOtp)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(code))
            throw new GraphQLException("hash y code son requeridos.");

        try
        {
            var result = await guestOtp.VerifyOtpByHashAsync(hash, code, name, phone, email);
            if (!result.Success || result.Data is null)
                return null;

            var d = result.Data;
            return new GuestDataType
            {
                Id = d.Id,
                Name = d.Name,
                Phone = d.Phone,
                Email = d.Email ?? "",
                SessionType = d.SessionType.ToString()
            };
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    /// <summary>
    /// Verifica el OTP y vincula (merge) las citas de la cuenta invitada con la misma
    /// cédula del usuario autenticado. Transfiere todas las citas del invitado a la cuenta.
    /// </summary>
    [Authorize]
    public async Task<MergeGuestResultType> MergeGuestAppointmentsAsync(
        string hash,
        string code,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IGuestOtpService guestOtp)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(code))
            throw new GraphQLException("hash y code son requeridos.");

        var userId = httpContextAccessor.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new GraphQLException("No autenticado");

        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted)
            ?? throw new GraphQLException("Usuario no encontrado.");

        if (string.IsNullOrWhiteSpace(user.IdentificationNumber))
            throw new GraphQLException("Tu cuenta no tiene cédula registrada.");

        var result = await guestOtp.MergeGuestAppointmentsAsync(
            hash, code, user.Id, user.IdentificationNumber);

        return new MergeGuestResultType
        {
            Success = result.Success,
            MergedCount = result.MergedCount,
            Error = result.Error
        };
    }

    private static OtpChannel ParseChannel(string channel) =>
        channel.ToLowerInvariant() switch
        {
            "whatsapp" => OtpChannel.WhatsApp,
            "email" => OtpChannel.Email,
            "sms" => OtpChannel.SMS,
            _ => throw new GraphQLException("Canal inválido. Usa 'whatsapp', 'email' o 'sms'.")
        };
}
