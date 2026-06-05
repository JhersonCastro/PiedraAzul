using System;
using System.Linq;
using System.Text.RegularExpressions;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Contracts.Validation
{
    /// <summary>
    /// Reglas de validación para datos colombianos (celular, documento, edad).
    /// Centralizado para reutilizar en cliente (DataAnnotations) y servidor (handlers).
    /// </summary>
    public static class ColombianValidation
    {
        public const int MinRegistrationAge = 13;

        /// <summary>
        /// Valida un celular colombiano: 10 dígitos empezando en 3, con prefijo +57/57 opcional.
        /// Acepta espacios, guiones y paréntesis (se ignoran).
        /// </summary>
        public static bool IsValidMobile(string? phone)
        {
            return NormalizeMobile(phone) is not null;
        }

        /// <summary>
        /// Normaliza un celular colombiano a 10 dígitos "3XXXXXXXXX".
        /// Devuelve null si no es un celular colombiano válido.
        /// </summary>
        public static string? NormalizeMobile(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // Con indicativo país: 57 + 10 dígitos
            if (digits.Length == 12 && digits.StartsWith("57"))
                digits = digits[2..];

            if (digits.Length == 10 && digits[0] == '3')
                return digits;

            return null;
        }

        /// <summary>
        /// Valida el número de documento según su tipo:
        /// - CC: solo dígitos, 6-10
        /// - TI: solo dígitos, 6-11 (incluye NUIP)
        /// - CE: alfanumérico, 5-12
        /// - PA: alfanumérico, 6-12
        /// </summary>
        public static bool IsValidDocumentNumber(DocumentType type, string? number)
        {
            if (string.IsNullOrWhiteSpace(number)) return false;
            var value = number.Trim();

            return type switch
            {
                DocumentType.CC => Regex.IsMatch(value, @"^\d{6,10}$"),
                DocumentType.TI => Regex.IsMatch(value, @"^\d{6,11}$"),
                DocumentType.CE => Regex.IsMatch(value, @"^[a-zA-Z0-9]{5,12}$"),
                DocumentType.PA => Regex.IsMatch(value, @"^[a-zA-Z0-9]{6,12}$"),
                _ => false
            };
        }

        /// <summary>Mensaje de error específico para el formato de documento según tipo.</summary>
        public static string DocumentNumberError(DocumentType type) => type switch
        {
            DocumentType.CC => "La cédula de ciudadanía debe tener entre 6 y 10 dígitos.",
            DocumentType.TI => "La tarjeta de identidad debe tener entre 6 y 11 dígitos.",
            DocumentType.CE => "La cédula de extranjería debe tener entre 5 y 12 caracteres alfanuméricos.",
            DocumentType.PA => "El pasaporte debe tener entre 6 y 12 caracteres alfanuméricos.",
            _ => "Número de documento inválido."
        };

        /// <summary>Calcula la edad cumplida a partir de la fecha de nacimiento.</summary>
        public static int? AgeFrom(DateTime? birthDate, DateTime? asOf = null)
        {
            if (birthDate is null) return null;
            var today = (asOf ?? DateTime.UtcNow).Date;
            var bd = birthDate.Value.Date;
            if (bd > today) return null;

            var age = today.Year - bd.Year;
            if (bd > today.AddYears(-age)) age--;
            return age;
        }

        /// <summary>True si la persona tiene al menos <paramref name="minAge"/> años cumplidos.</summary>
        public static bool IsAtLeastAge(DateTime? birthDate, int minAge)
        {
            var age = AgeFrom(birthDate);
            return age is not null && age >= minAge;
        }

        /// <summary>
        /// Valida que el tipo de documento sea coherente con la edad, según el estándar colombiano:
        /// - CC  → 18+ años
        /// - TI  → 7–17 años
        /// - CE / PA → cualquier edad (aplica a extranjeros, sin restricción)
        /// </summary>
        public static bool IsDocumentTypeValidForAge(DocumentType type, DateTime? birthDate)
        {
            var age = AgeFrom(birthDate);
            if (age is null) return true; // sin fecha no se puede validar

            return type switch
            {
                DocumentType.CC => age >= 18,
                DocumentType.TI => age >= 7 && age <= 17,
                DocumentType.CE => true,  // sin restricción de edad para extranjeros
                DocumentType.PA => true,
                _ => true
            };
        }

        /// <summary>
        /// Devuelve el tipo de documento esperado según la edad del usuario colombiano.
        /// CE/PA se sugieren solo para extranjeros; este método asume ciudadanos colombianos.
        /// </summary>
        public static DocumentType ExpectedDocumentType(DateTime? birthDate)
        {
            var age = AgeFrom(birthDate);
            return age switch
            {
                >= 18 => DocumentType.CC,
                >= 7  => DocumentType.TI,
                _     => DocumentType.TI  // menores de 7 usarían RC, pero no está en el enum
            };
        }

        /// <summary>Mensaje de error cuando el documento no corresponde a la edad.</summary>
        public static string DocumentTypeAgeError(DocumentType type, int? age) => type switch
        {
            DocumentType.CC => $"La cédula de ciudadanía (CC) es para mayores de 18 años. " +
                               $"Con {age} años debes usar Tarjeta de Identidad (TI).",
            DocumentType.TI => $"La tarjeta de identidad (TI) es para personas de 7 a 17 años. " +
                               $"Con {age} años debes usar Cédula de Ciudadanía (CC).",
            _ => "El tipo de documento no corresponde a la edad indicada."
        };
    }
}
