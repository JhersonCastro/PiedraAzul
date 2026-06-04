using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Contracts.Validation
{
    /// <summary>Valida que el valor sea un celular colombiano (10 dígitos empezando en 3).</summary>
    public sealed class ColombianMobileAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var phone = value as string;

            // El [Required] se encarga de los vacíos; aquí solo validamos el formato si hay valor.
            if (string.IsNullOrWhiteSpace(phone))
                return ValidationResult.Success;

            return ColombianValidation.IsValidMobile(phone)
                ? ValidationResult.Success
                : new ValidationResult(
                    ErrorMessage ?? "Ingresa un celular colombiano válido (10 dígitos, empieza por 3).",
                    new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}
