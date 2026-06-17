using System.ComponentModel.DataAnnotations;
using PiedraAzul.Client.Models.GraphQL;

namespace PiedraAzul.Client.Models.UserProfiles;

public class MFAVerificationModel
{
    [Required(ErrorMessage = "El código es requerido")]
    public string? OTP { get; set; }
}

public class LoginResultModel
{
    public UserGQL? User { get; set; }
    public MFARequired? MFARequired { get; set; }
}

public class MFARequired
{
    public string? MFAToken { get; set; }
    public string? MFAMethod { get; set; }
    public bool HasEmail { get; set; }
}
