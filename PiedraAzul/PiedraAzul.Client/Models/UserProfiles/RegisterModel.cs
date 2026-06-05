using PiedraAzul.Contracts.Enums;
using PiedraAzul.Contracts.Validation;
using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.UserProfiles
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Selecciona el tipo de documento")]
        [DocumentTypeForAge(nameof(BirthDate))]
        public DocumentType DocumentType { get; set; } = DocumentType.CC;

        [Required(ErrorMessage = "El número de documento es requerido")]
        [DocumentNumber(nameof(DocumentType))]
        public string Document { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [ColombianMobile]
        public string Phone { get; set; }

        [Required]
        [Range(1, 3, ErrorMessage = "Seleccione un género válido")]
        public GenderType Gender { get; set; } = GenderType.NonSpecified;

        [Required(ErrorMessage = "La fecha de nacimiento es requerida")]
        [MinimumAge(13, ErrorMessage = "Debes tener al menos 13 años para registrarte")]
        public DateTime? BirthDate { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }
    }
}
