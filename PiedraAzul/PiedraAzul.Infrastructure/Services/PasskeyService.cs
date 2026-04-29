using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Infrastructure.Auth;
using PiedraAzul.Infrastructure.Identity;
using PiedraAzul.Infrastructure.Persistence;
using System.Text;
using System.Text.Json;

namespace PiedraAzul.Infrastructure.Services;

public class PasskeyService(
    IFido2 fido2,
    AppDbContext context,
    UserManager<ApplicationUser> userManager,
    IMemoryCache cache
) : IPasskeyService
{
    private static readonly MemoryCacheEntryOptions ChallengeExpiry =
        new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<string> BeginRegistrationAsync(string userId, string email, string displayName)
    {
        var user = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(userId),
            Name = email,
            DisplayName = displayName
        };

        var existingCredentials = await context.PasskeyCredentials
            .Where(c => c.UserId == userId)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToListAsync();

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = existingCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Preferred
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        cache.Set($"passkey:reg:{userId}", options, ChallengeExpiry);
        return options.ToJson();
    }

    public async Task<bool> CompleteRegistrationAsync(string userId, string attestationResponseJson, string friendlyName)
    {
        if (!cache.TryGetValue($"passkey:reg:{userId}", out CredentialCreateOptions? storedOptions) || storedOptions is null)
            throw new InvalidOperationException("Sesión expirada. Por favor, inicia el registro nuevamente.");

        var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(
            attestationResponseJson, JsonOpts)
            ?? throw new InvalidOperationException("Respuesta de autenticador inválida. Verifica tu dispositivo.");

        try
        {
            var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationResponse,
                OriginalOptions = storedOptions,
                IsCredentialIdUniqueToUserCallback = async (args, ct) =>
                    !await context.PasskeyCredentials
                        .AnyAsync(c => c.CredentialId.SequenceEqual(args.CredentialId), ct)
            });

            cache.Remove($"passkey:reg:{userId}");

            context.PasskeyCredentials.Add(new PasskeyCredential(
                userId,
                result.Id,
                result.PublicKey,
                result.SignCount,
                friendlyName));

            await context.SaveChangesAsync();
            return true;
        }
        catch (Fido2VerificationException ex)
        {
            throw new InvalidOperationException($"Verificación fallida: {ex.Message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al registrar passkey: {ex.Message}");
        }
    }

    public Task<string> BeginAssertionAsync()
    {
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = [],
            UserVerification = UserVerificationRequirement.Required
        });
        cache.Set("passkey:assertion", options, ChallengeExpiry);
        return Task.FromResult(options.ToJson());
    }

    public async Task<List<PasskeyDto>> GetUserPasskeysAsync(string userId)
        => await context.PasskeyCredentials
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new PasskeyDto(c.Id, c.FriendlyName, c.CreatedAt))
            .ToListAsync();

    public async Task<bool> DeletePasskeyAsync(string userId, Guid passkeyId)
    {
        var credential = await context.PasskeyCredentials
            .FirstOrDefaultAsync(c => c.Id == passkeyId && c.UserId == userId);

        if (credential is null) return false;

        context.PasskeyCredentials.Remove(credential);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<(string UserId, List<string> Roles)> CompleteAssertionAsync(string assertionResponseJson)
    {
        if (!cache.TryGetValue("passkey:assertion", out AssertionOptions? storedOptions) || storedOptions is null)
            throw new InvalidOperationException("No hay challenge activo. Inicia el proceso nuevamente.");

        var assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(
            assertionResponseJson, JsonOpts)
            ?? throw new InvalidOperationException("Respuesta de assertion inválida");

        var credentialId = assertionResponse.RawId;

        var credential = await context.PasskeyCredentials
            .FirstOrDefaultAsync(c => c.CredentialId.SequenceEqual(credentialId))
            ?? throw new InvalidOperationException("Credencial no encontrada");

        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            OriginalOptions = storedOptions,
            StoredPublicKey = credential.PublicKey,
            StoredSignatureCounter = credential.SignatureCounter,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
            {
                var cred = await context.PasskeyCredentials
                    .FirstOrDefaultAsync(c => c.CredentialId.SequenceEqual(args.CredentialId), ct);
                return cred?.UserId == Encoding.UTF8.GetString(args.UserHandle);
            }
        });

        cache.Remove("passkey:assertion");

        credential.UpdateCounter(result.SignCount);
        await context.SaveChangesAsync();

        var appUser = await userManager.FindByIdAsync(credential.UserId)
            ?? throw new InvalidOperationException("Usuario no encontrado");

        var roles = await userManager.GetRolesAsync(appUser);
        return (credential.UserId, roles.ToList());
    }
}
