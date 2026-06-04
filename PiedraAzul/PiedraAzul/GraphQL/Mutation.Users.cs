using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using PiedraAzul.Application.Common.Caching;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Contracts.Validation;
using PiedraAzul.Domain.Entities.Profiles.Doctor;
using PiedraAzul.Domain.Entities.Profiles.Patients;
using PiedraAzul.Domain.Repositories;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Infrastructure.Identity;
using System.Security.Cryptography;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    [Authorize(Roles = new[] { "Admin" })]
    public async Task<UserType> CreateUserAsync(
        CreateUserInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IDoctorRepository doctorRepository,
        [Service] IPatientRepository patientRepository,
        [Service] IUnitOfWork unitOfWork,
        [Service] IEmailService emailService,
        [Service] ICacheService cache,
        [Service] ILogger<Mutation> logger)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            throw new GraphQLException("El nombre completo es requerido");

        if (string.IsNullOrWhiteSpace(input.Email))
            throw new GraphQLException("El correo es requerido");

        var existing = await userManager.FindByEmailAsync(input.Email);
        if (existing is not null)
            throw new GraphQLException("Este correo ya está registrado");

        // ── Validación colombiana (servidor) ──────────────────────────────────
        if (string.IsNullOrWhiteSpace(input.IdentificationNumber))
            throw new GraphQLException("La cédula / documento es requerido");

        if (!ColombianValidation.IsValidDocumentNumber(input.DocumentType, input.IdentificationNumber))
            throw new GraphQLException(ColombianValidation.DocumentNumberError(input.DocumentType));

        var normalizedPhone = ColombianValidation.NormalizeMobile(input.Phone);
        if (normalizedPhone is null)
            throw new GraphQLException("Ingresa un celular colombiano válido (10 dígitos, empieza por 3).");

        if (!ColombianValidation.IsAtLeastAge(input.BirthDate, ColombianValidation.MinRegistrationAge))
            throw new GraphQLException($"El usuario debe tener al menos {ColombianValidation.MinRegistrationAge} años.");

        var identification = input.IdentificationNumber.Trim();
        var dupId = await userManager.Users.FirstOrDefaultAsync(
            u => u.IdentificationNumber == identification && !u.IsDeleted);
        if (dupId is not null)
            throw new GraphQLException("Ya existe un usuario con esta cédula / documento");

        var tempPassword = GenerateTempPassword();

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            Name = input.FullName,
            PhoneNumber = normalizedPhone,
            IdentificationNumber = identification,
            DocumentType = (Domain.Entities.Shared.Enums.DocumentType)(int)input.DocumentType,
            Gender = (Domain.Entities.Shared.Enums.GenderType)(int)input.Gender,
            BirthDate = input.BirthDate,
            EmailConfirmed = true,
        };

        if (!input.IsActive)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }

        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
            throw new GraphQLException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, input.Role);

        if (input.Role == "Doctor")
        {
            var specialty = (Domain.Entities.Shared.Enums.DoctorType)(input.DoctorType ?? DoctorSpecialty.NaturalMedicine);
            await unitOfWork.ExecuteAsync(async ct =>
            {
                var doctor = new Doctor(user.Id, specialty, "", "");
                await doctorRepository.AddAsync(doctor, ct);
                return true;
            });

            // Nuevo doctor → invalidar la lista de su especialidad.
            cache.RemoveByTag(CacheKeys.TagSpecialty((int)specialty));
        }
        else if (input.Role == "Patient")
        {
            await unitOfWork.ExecuteAsync(async ct =>
            {
                var patient = new RegisteredPatient(user.Id, user.Name);
                await patientRepository.AddAsync(patient, ct);
                return true;
            });
        }

        _ = emailService.SendWelcomeWithPasswordAsync(input.Email, input.FullName, tempPassword, input.Role)
            .ContinueWith(t =>
            {
                if (t.IsFaulted)
                    logger.LogWarning("No se pudo enviar el correo de bienvenida a {Email}: {Error}",
                        input.Email, t.Exception?.Message);
                else
                    logger.LogInformation("Correo de bienvenida enviado a {Email}", input.Email);
            });

        var now = DateTimeOffset.UtcNow;
        var isActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now;

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            IdentificationNumber = user.IdentificationNumber,
            DocumentType = (PiedraAzul.Contracts.Enums.DocumentType)(int)user.DocumentType,
            Gender = (PiedraAzul.Contracts.Enums.GenderType)(int)user.Gender,
            BirthDate = user.BirthDate,
            AvatarUrl = user.AvatarUrl,
            Roles = [input.Role],
            DoctorType = input.Role == "Doctor" ? input.DoctorType : null,
            IsActive = isActive,
            CreatedAt = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed,
            TempPassword = tempPassword
        };
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<UserType> UpdateUserAsync(
        UpdateUserInput input,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IDoctorRepository doctorRepository,
        [Service] IUnitOfWork unitOfWork,
        [Service] ICacheService cache)
    {
        var user = await userManager.FindByIdAsync(input.UserId)
            ?? throw new GraphQLException("Usuario no encontrado");

        if (user.IsDeleted)
            throw new GraphQLException("No se puede editar un usuario eliminado");

        if (!string.IsNullOrWhiteSpace(input.FullName))
            user.Name = input.FullName;

        if (!string.IsNullOrWhiteSpace(input.Email) && input.Email != user.Email)
        {
            var dup = await userManager.FindByEmailAsync(input.Email);
            if (dup is not null && dup.Id != user.Id)
                throw new GraphQLException("Este correo ya está en uso");

            user.Email = input.Email;
            user.UserName = input.Email;
            user.NormalizedEmail = userManager.NormalizeEmail(input.Email);
            user.NormalizedUserName = userManager.NormalizeName(input.Email);
        }

        if (input.Phone is not null)
        {
            var normalizedPhone = ColombianValidation.NormalizeMobile(input.Phone);
            if (normalizedPhone is null)
                throw new GraphQLException("Ingresa un celular colombiano válido (10 dígitos, empieza por 3).");
            user.PhoneNumber = normalizedPhone;
        }

        if (input.DocumentType.HasValue)
            user.DocumentType = (Domain.Entities.Shared.Enums.DocumentType)(int)input.DocumentType.Value;

        if (!string.IsNullOrWhiteSpace(input.IdentificationNumber))
        {
            var identification = input.IdentificationNumber.Trim();
            var docType = input.DocumentType ?? (Contracts.Enums.DocumentType)(int)user.DocumentType;

            if (!ColombianValidation.IsValidDocumentNumber(docType, identification))
                throw new GraphQLException(ColombianValidation.DocumentNumberError(docType));

            if (identification != user.IdentificationNumber)
            {
                var dupId = await userManager.Users.FirstOrDefaultAsync(
                    u => u.IdentificationNumber == identification && !u.IsDeleted);
                if (dupId is not null && dupId.Id != user.Id)
                    throw new GraphQLException("Ya existe un usuario con esta cédula / documento");
            }
            user.IdentificationNumber = identification;
        }

        if (input.Gender.HasValue)
            user.Gender = (Domain.Entities.Shared.Enums.GenderType)(int)input.Gender.Value;

        if (input.BirthDate.HasValue)
        {
            if (!ColombianValidation.IsAtLeastAge(input.BirthDate, ColombianValidation.MinRegistrationAge))
                throw new GraphQLException($"El usuario debe tener al menos {ColombianValidation.MinRegistrationAge} años.");
            user.BirthDate = input.BirthDate;
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new GraphQLException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(input.Role))
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            if (!currentRoles.Contains(input.Role))
            {
                await userManager.RemoveFromRolesAsync(user, currentRoles);
                await userManager.AddToRoleAsync(user, input.Role);
            }
        }

        if (input.DoctorType.HasValue)
        {
            var doctor = await doctorRepository.GetByIdAsync(user.Id);
            if (doctor is not null)
            {
                var oldSpecialty = doctor.Specialty;
                var newSpecialty = (Domain.Entities.Shared.Enums.DoctorType)input.DoctorType.Value;

                await unitOfWork.ExecuteAsync(async ct =>
                {
                    var newDoctor = new Doctor(
                        user.Id,
                        newSpecialty,
                        doctor.LicenseNumber,
                        doctor.Notes ?? "");
                    await doctorRepository.UpdateAsync(newDoctor, ct);
                    return true;
                });

                // Cambió de especialidad → invalidar ambas listas.
                cache.RemoveByTag(CacheKeys.TagSpecialty((int)oldSpecialty));
                cache.RemoveByTag(CacheKeys.TagSpecialty((int)newSpecialty));
            }
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var now = DateTimeOffset.UtcNow;
        var isActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now;

        DoctorSpecialty? doctorSpecialty = null;
        if (roles.Contains("Doctor"))
        {
            var doc = await doctorRepository.GetByIdAsync(user.Id);
            if (doc is not null)
                doctorSpecialty = (DoctorSpecialty)doc.Specialty;
        }

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            IdentificationNumber = user.IdentificationNumber,
            DocumentType = (PiedraAzul.Contracts.Enums.DocumentType)(int)user.DocumentType,
            Gender = (PiedraAzul.Contracts.Enums.GenderType)(int)user.Gender,
            BirthDate = user.BirthDate,
            AvatarUrl = user.AvatarUrl,
            Roles = roles,
            DoctorType = doctorSpecialty,
            IsActive = isActive,
            CreatedAt = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed
        };
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<bool> DeleteUserAsync(
        string userId,
        [Service] UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new GraphQLException("Usuario no encontrado");

        if (user.IsDeleted)
            throw new GraphQLException("El usuario ya fue eliminado");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new GraphQLException("No se pudo eliminar el usuario");

        await userManager.UpdateSecurityStampAsync(user);
        return true;
    }

    [Authorize(Roles = new[] { "Admin" })]
    public async Task<UserType> ToggleUserStatusAsync(
        string userId,
        [Service] UserManager<ApplicationUser> userManager,
        [Service] IDoctorRepository doctorRepository)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new GraphQLException("Usuario no encontrado");

        if (user.IsDeleted)
            throw new GraphQLException("No se puede cambiar el estado de un usuario eliminado");

        var now = DateTimeOffset.UtcNow;
        var isCurrentlyActive = !user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now;

        if (isCurrentlyActive)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await userManager.UpdateAsync(user);
            await userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            user.LockoutEnabled = false;
            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
        }

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var newIsActive = !isCurrentlyActive;

        DoctorSpecialty? doctorSpecialty = null;
        if (roles.Contains("Doctor"))
        {
            var doc = await doctorRepository.GetByIdAsync(user.Id);
            if (doc is not null)
                doctorSpecialty = (DoctorSpecialty)doc.Specialty;
        }

        return new UserType
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber ?? "",
            AvatarUrl = user.AvatarUrl,
            Roles = roles,
            DoctorType = doctorSpecialty,
            IsActive = newIsActive,
            CreatedAt = user.CreatedAt,
            EmailConfirmed = user.EmailConfirmed
        };
    }

    private static string GenerateTempPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
            .ToArray());
    }
}
