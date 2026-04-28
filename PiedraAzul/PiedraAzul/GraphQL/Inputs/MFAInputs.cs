using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.GraphQL.Inputs;

public class EnableMFAInput
{
    [Required(ErrorMessage = "El método de MFA es requerido")]
    public required string Method { get; set; } // "Email" or "TOTP"
}

public class VerifyMFAInput
{
    [Required(ErrorMessage = "El código OTP es requerido")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código OTP debe ser de 6 dígitos")]
    public required string OTP { get; set; }
}

public class DisableMFAInput
{
    [Required(ErrorMessage = "El método de MFA es requerido")]
    public required string Method { get; set; }

    [Required(ErrorMessage = "La confirmación es requerida")]
    public required bool Confirm { get; set; }
}

public class BeginTOTPSetupInput
{
    [Required(ErrorMessage = "El correo electrónico es requerido")]
    [EmailAddress(ErrorMessage = "Correo electrónico inválido")]
    public required string Email { get; set; }
}

public class ConfirmTOTPSetupInput
{
    [Required(ErrorMessage = "El código TOTP es requerido")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código TOTP debe ser de 6 dígitos")]
    public required string TOTP { get; set; }
}

public class VerifyMFALoginInput
{
    [Required(ErrorMessage = "El token de MFA es requerido")]
    public required string MFAToken { get; set; }

    [Required(ErrorMessage = "El código es requerido")]
    public required string OTP { get; set; }
}
