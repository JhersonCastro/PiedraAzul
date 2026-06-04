using System;
using System.ComponentModel.DataAnnotations;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Contracts.Validation
{
    /// <summary>
    /// Valida el número de documento según el tipo de documento de una propiedad hermana.
    /// Uso: [DocumentNumber(nameof(DocumentType))] sobre la propiedad del número.
    /// </summary>
    public sealed class DocumentNumberAttribute : ValidationAttribute
    {
        private readonly string _documentTypeProperty;

        public DocumentNumberAttribute(string documentTypeProperty)
        {
            _documentTypeProperty = documentTypeProperty;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var number = value as string;
            if (string.IsNullOrWhiteSpace(number))
                return ValidationResult.Success; // [Required] maneja el vacío

            var typeProp = validationContext.ObjectType.GetProperty(_documentTypeProperty);
            if (typeProp is null)
                return new ValidationResult($"No se encontró la propiedad '{_documentTypeProperty}'.");

            var rawType = typeProp.GetValue(validationContext.ObjectInstance);
            if (rawType is not DocumentType docType)
                return ValidationResult.Success; // sin tipo válido, no podemos validar el formato

            return ColombianValidation.IsValidDocumentNumber(docType, number)
                ? ValidationResult.Success
                : new ValidationResult(
                    ErrorMessage ?? ColombianValidation.DocumentNumberError(docType),
                    new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}
