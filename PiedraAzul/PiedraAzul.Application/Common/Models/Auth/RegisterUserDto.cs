using System;
using PiedraAzul.Domain.Entities.Shared.Enums;

namespace PiedraAzul.Application.Common.Models.Auth
{
    public record RegisterUserDto(
        string Email,
        string Name,
        string? PhoneNumber,
        string? IdentificationNumber,
        DocumentType DocumentType = DocumentType.CC,
        GenderType Gender = GenderType.NonSpecified,
        DateTime? BirthDate = null
    );
}
