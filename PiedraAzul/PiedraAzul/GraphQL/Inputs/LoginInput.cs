using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.GraphQL.Inputs;

public record LoginInput(
    [Required(ErrorMessage = "El correo es requerido")]
    [EmailAddress(ErrorMessage = "El correo no es válido")]
    string Email,

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 255 caracteres")]
    string Password
);
