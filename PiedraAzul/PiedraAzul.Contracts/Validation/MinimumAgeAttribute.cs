using System;
using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Contracts.Validation
{
    /// <summary>Valida que una fecha de nacimiento corresponda a una edad mínima (en años cumplidos).</summary>
    public sealed class MinimumAgeAttribute : ValidationAttribute
    {
        public int MinAge { get; }

        public MinimumAgeAttribute(int minAge)
        {
            MinAge = minAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success; // dejar que [Required] maneje el vacío

            if (value is not DateTime birthDate)
                return new ValidationResult("Fecha inválida.", new[] { validationContext.MemberName ?? string.Empty });

            if (birthDate.Date > DateTime.UtcNow.Date)
                return new ValidationResult(
                    ErrorMessage ?? "La fecha de nacimiento no puede ser futura.",
                    new[] { validationContext.MemberName ?? string.Empty });

            return ColombianValidation.IsAtLeastAge(birthDate, MinAge)
                ? ValidationResult.Success
                : new ValidationResult(
                    ErrorMessage ?? $"Debes tener al menos {MinAge} años.",
                    new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}
