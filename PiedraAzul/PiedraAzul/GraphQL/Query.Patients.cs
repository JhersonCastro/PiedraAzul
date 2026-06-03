using HotChocolate;
using HotChocolate.Authorization;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Features.Patients.Queries.SearchPatients;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.Infrastructure.Identity;

namespace PiedraAzul.GraphQL;

public partial class Query
{
    /// <summary>
    /// Lookup público (sin auth) para el flujo de auto-agendamiento.
    /// Devuelve un hash de verificación junto con canales enmascarados (teléfono/email).
    /// El usuario debe verificar OTP para acceder a los datos completos.
    /// - REGISTERED: usuario con cuenta. Si tiene email/teléfono, genera hash; si no, Type=NoContact.
    /// - GUEST: guest existente con misma cédula. Genera hash si tiene email/teléfono.
    /// - null: no existe.
    /// </summary>
    public async Task<GuestLookupResultType?> LookupGuestByIdentificationAsync(
        string identification,
        [Service] IPatientGuestRepository guestRepository,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IGuestOtpService guestOtpService)
    {
        if (string.IsNullOrWhiteSpace(identification))
            return null;

        var id = identification.Trim();

        // 1. Buscar en usuarios registrados
        var registeredUser = await userManager.Users
            .FirstOrDefaultAsync(u => u.IdentificationNumber == id && !u.IsDeleted);

        if (registeredUser is not null)
        {
            var hasPhone = !string.IsNullOrEmpty(registeredUser.PhoneNumber);
            var hasEmail = !string.IsNullOrEmpty(registeredUser.Email);

            // Si no tiene ni teléfono ni email, no puede continuar como invitado
            if (!hasPhone && !hasEmail)
            {
                return new GuestLookupResultType
                {
                    VerificationHash = "",
                    HasPhone = false,
                    HasEmail = false,
                    MaskedPhone = null,
                    MaskedEmail = null,
                    Type = PatientTypeEnum.NoContact
                };
            }

            // Crear sesión para usuario registrado
            var userHash = await guestOtpService.CreateSessionForRegisteredUserAsync(
                registeredUser.Id,
                registeredUser.Name,
                registeredUser.PhoneNumber,
                registeredUser.Email,
                expirationMinutes: 0);

            return new GuestLookupResultType
            {
                VerificationHash = userHash,
                HasPhone = hasPhone,
                HasEmail = hasEmail,
                MaskedPhone = hasPhone ? MaskPhone(registeredUser.PhoneNumber!) : null,
                MaskedEmail = hasEmail ? MaskEmail(registeredUser.Email!) : null,
                Type = PatientTypeEnum.Registered
            };
        }

        // 2. Buscar en pacientes invitados
        var guest = await guestRepository.GetByIdAsync(id);
        if (guest is null) return null;

        var guestHasPhone = !string.IsNullOrEmpty(guest.Phone);
        var guestHasEmail = !string.IsNullOrEmpty(guest.Email);

        // Si no tiene ni teléfono ni email, no puede continuar
        if (!guestHasPhone && !guestHasEmail)
        {
            return new GuestLookupResultType
            {
                VerificationHash = "",
                HasPhone = false,
                HasEmail = false,
                MaskedPhone = null,
                MaskedEmail = null,
                Type = PatientTypeEnum.NoContact
            };
        }

        var hash = await guestOtpService.CreateSessionAsync(guest.Id, expirationMinutes: 0);

        return new GuestLookupResultType
        {
            VerificationHash = hash,
            HasPhone = guestHasPhone,
            HasEmail = guestHasEmail,
            MaskedPhone = guestHasPhone ? MaskPhoneShort(guest.Phone) : null,
            MaskedEmail = guestHasEmail ? MaskEmail(guest.Email!) : null,
            Type = PatientTypeEnum.Guest
        };
    }

    /// <summary>
    /// Enmascara el teléfono con formato "Terminado en: XXXX".
    /// </summary>
    private static string MaskPhoneShort(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "****";
        return $"Terminado en: {digits[^4..]}";
    }

    /// <summary>
    /// Enmascara el email: muestra primera letra, asteriscos, última letra antes del @ y el dominio.
    /// </summary>
    /// <example>"juanperez@gmail.com" → "j***z@gmail.com"</example>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return "***@***";

        var parts = email.Split('@');
        var local = parts[0];
        var domain = parts[1];

        if (local.Length <= 2)
            return $"{local[0]}***@{domain}";

        return $"{local[0]}***{local[^1]}@{domain}";
    }

    [Authorize(Roles = new[] { "Doctor", "Admin" })]
    public async Task<List<PatientSearchResultType>> SearchPatientsAsync(
        string query,
        int? limit,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager)
    {
        var patients = await mediator.Send(new SearchPatientsQuery(query));
        var deduplicated = patients
            .GroupBy(p => p.Id)
            .Select(g => g.First())
            .OrderBy(p => p.Name)
            .Take(limit ?? int.MaxValue)
            .ToList();

        // ✅ Cargar todos los usuarios de una vez (evita N+1)
        var registeredIds = deduplicated
            .Where(p => p.Type == "Registered")
            .Select(p => p.Id)
            .ToList();

        var usersMap = registeredIds.Any()
            ? (await userManager.Users
                .Where(u => registeredIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id))
            : new();

        var results = new List<PatientSearchResultType>();
        foreach (var p in deduplicated)
        {
            var phone = p.Phone;
            if (p.Type == "Registered" && string.IsNullOrEmpty(phone) && usersMap.TryGetValue(p.Id, out var user))
            {
                phone = user.PhoneNumber ?? "";
            }

            var identification = p.Type == "Guest" ? p.Id : "";
            if (p.Type == "Registered" && usersMap.TryGetValue(p.Id, out var u))
            {
                identification = u.IdentificationNumber ?? "";
            }

            results.Add(new PatientSearchResultType
            {
                Id = p.Id,
                Name = p.Name,
                Identification = identification,
                Phone = phone,
                Type = p.Type == "Guest" ? PatientTypeEnum.Guest : PatientTypeEnum.Registered
            });
        }
        return results;
    }

    [Authorize(Roles = new[] { "Doctor", "Admin" })]
    public async Task<List<PatientSearchResultType>> SearchAutoCompletePatientsAsync(
        string query,
        [Service] IMediator mediator,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IPatientGuestRepository guestRepository)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];
        var q = query.Trim();

        // ── 1. Buscar pacientes registrados por nombre EN la tabla Patients (mediator) ──
        var patientDtos = await mediator.Send(new SearchPatientsQuery(q));
        var registeredIdsFromName = patientDtos
            .Where(p => p.Type == "Registered")
            .Select(p => p.Id)
            .Distinct()
            .ToList();

        // ── 2. Buscar directamente en AspNetUsers por identificación, email o teléfono ──
        //    Cubre casos donde el nombre coincide con la tabla Patients pero la búsqueda
        //    es por cédula/email/teléfono que solo viven en Identity.
        var usersFromIdentity = await userManager.Users
            .Where(u => !u.IsDeleted && (
                EF.Functions.ILike(u.IdentificationNumber ?? "", $"%{q}%") ||
                EF.Functions.ILike(u.Email ?? "", $"%{q}%") ||
                EF.Functions.ILike(u.PhoneNumber ?? "", $"%{q}%") ||
                EF.Functions.ILike(u.Name, $"%{q}%")))
            .Take(15)
            .ToListAsync();

        // ── 3. Unir IDs únicos y cargar datos de Identity para todos ──
        var allRegisteredIds = registeredIdsFromName
            .Union(usersFromIdentity.Select(u => u.Id))
            .Distinct()
            .ToList();

        var usersMap = allRegisteredIds.Any()
            ? await userManager.Users
                .Where(u => allRegisteredIds.Contains(u.Id) && !u.IsDeleted)
                .ToDictionaryAsync(u => u.Id)
            : new();

        var results = new List<PatientSearchResultType>();
        var addedIds = new HashSet<string>();

        foreach (var uid in allRegisteredIds)
        {
            if (!addedIds.Add(uid)) continue;
            usersMap.TryGetValue(uid, out var appUser);
            results.Add(new PatientSearchResultType
            {
                Id             = uid,
                Name           = appUser?.Name ?? uid,
                Identification = appUser?.IdentificationNumber ?? "",
                Phone          = appUser?.PhoneNumber ?? "",
                Type           = PatientTypeEnum.Registered
            });
        }

        // ── 4. Invitados: búsqueda case-insensitive por nombre, teléfono e identificación ──
        var guestDtos = await guestRepository.SearchAsync(q);

        // Índice de identificaciones de registrados para deduplicar
        var registeredIdentifications = results
            .Where(r => !string.IsNullOrEmpty(r.Identification))
            .Select(r => r.Identification)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var g in guestDtos)
        {
            // Si la identificación del invitado coincide con un registrado → skip (misma persona)
            if (registeredIdentifications.Contains(g.Id)) continue;
            if (!addedIds.Add(g.Id)) continue;

            results.Add(new PatientSearchResultType
            {
                Id             = g.Id,
                Name           = g.Name,
                Identification = g.Id,
                Phone          = g.Phone ?? "",
                Type           = PatientTypeEnum.Guest
            });
        }

        return results
            .OrderBy(r => r.Name)
            .Take(10)
            .ToList();
    }
    /// <summary>
    /// Enmascara el nombre: muestra los primeros 2 nombres, máximo 3 caracteres visibles
    /// por palabra, el resto como asteriscos según la longitud real de la palabra.
    /// </summary>
    /// <example>"Pepito Juarez Alcantarez" → "Pep**** Jua***"</example>
    private static string MaskName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "****";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var masked = parts
            .Take(2)
            .Select(word =>
            {
                var visible = Math.Min(3, word.Length);
                return word[..visible] + new string('*', word.Length - visible);
            });
        return string.Join(" ", masked);
    }

    /// <summary>Enmascara el teléfono: muestra solo los últimos 4 dígitos.</summary>
    /// <example>"31234567890" → "****7890"</example>
    private static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return "****";
        return "****" + digits[^4..];
    }
}
