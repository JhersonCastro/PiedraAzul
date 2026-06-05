using System;
using System.ComponentModel.DataAnnotations;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Contracts.Validation
{
    /// <summary>
    /// Valida que el tipo de documento sea coherente con la edad, según el estándar colombiano:
    /// - CC  → 18+ años
    /// - TI  → 7–17 años
    /// - CE / PA → sin restricción de edad (extranjeros)
    ///
    /// Uso: se aplica sobre la propiedad DocumentType, indicando el nombre
    /// de la propiedad BirthDate con la que se hace la validación cruzada.
    ///
    /// Ejemplo:
    ///   [DocumentTypeForAge(nameof(BirthDate))]
    ///   public DocumentType DocumentType { get; set; }
    /// </summary>
    public sealed class DocumentTypeForAgeAttribute : ValidationAttribute
    {
        private readonly string _birthDateProperty;

        public DocumentTypeForAgeAttribute(string birthDateProperty)
        {
            _birthDateProperty = birthDateProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
        {
            if (value is not DocumentType docType)
                return ValidationResult.Success;

            // CE y PA no tienen restricción de edad.
            if (docType is DocumentType.CE or DocumentType.PA)
                return ValidationResult.Success;

            var prop = ctx.ObjectType.GetProperty(_birthDateProperty);
            if (prop is null)
                return new ValidationResult($"No se encontró la propiedad '{_birthDateProperty}'.");

            var rawDate = prop.GetValue(ctx.ObjectInstance);
            DateTime? birthDate = rawDate is DateTime dt ? dt : (DateTime?)null;

            // Si aún no hay fecha, la validación de [Required] se encarga.
            if (birthDate is null)
                return ValidationResult.Success;

            if (ColombianValidation.IsDocumentTypeValidForAge(docType, birthDate))
                return ValidationResult.Success;

            var age = ColombianValidation.AgeFrom(birthDate);
            return new ValidationResult(
                ErrorMessage ?? ColombianValidation.DocumentTypeAgeError(docType, age),
                new[] { ctx.MemberName ?? string.Empty });
        }
    }
}
