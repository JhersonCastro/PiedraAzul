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
    [Required(ErrorMessage = "La confirmación es requerida")]
    public required bool Confirm { get; set; }
}
