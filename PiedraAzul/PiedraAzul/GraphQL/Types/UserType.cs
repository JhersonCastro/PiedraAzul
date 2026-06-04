using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.GraphQL.Types;

public class UserType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string IdentificationNumber { get; set; } = "";
    public DocumentType DocumentType { get; set; } = DocumentType.CC;
    public GenderType Gender { get; set; } = GenderType.NonSpecified;
    public DateTime? BirthDate { get; set; }
    public string AvatarUrl { get; set; } = "default.png";
    public List<string> Roles { get; set; } = new();
    public DoctorSpecialty? DoctorType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Solo presente al crear un usuario nuevo vía admin.</summary>
    public string? TempPassword { get; set; }
}

public class AuthResponseType
{
    public string AccessToken { get; set; } = "";
    public UserType User { get; set; } = new();
}
