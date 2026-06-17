using PiedraAzul.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.UserProfiles
{
    public class RegisterModel
    {
        [Required]
        public string Document { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Phone { get; set; }

        [Required]
        [Range(1, 3, ErrorMessage = "Seleccione un género válido")]
        public GenderType Gender { get; set; } = GenderType.NonSpecified;

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
