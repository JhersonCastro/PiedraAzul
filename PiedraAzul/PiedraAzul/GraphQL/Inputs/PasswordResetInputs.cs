using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.GraphQL.Inputs;

public class RequestPasswordResetInput
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
    public required string Email { get; set; }
}

public class ResetPasswordInput
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "El token es requerido")]
    public required string Token { get; set; }

    [Required(ErrorMessage = "La contraseña es requerida")]
    [StringLength(128, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public required string NewPassword { get; set; }

    [Required(ErrorMessage = "Confirmar contraseña es requerido")]
    [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
    public required string ConfirmPassword { get; set; }
}
