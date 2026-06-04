using PiedraAzul.Contracts.Enums;
using PiedraAzul.GraphQL.Types;

namespace PiedraAzul.GraphQL.Inputs;

public class CreateUserInput
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string IdentificationNumber { get; set; } = "";
    public DocumentType DocumentType { get; set; } = DocumentType.CC;
    public GenderType Gender { get; set; } = GenderType.NonSpecified;
    public DateTime? BirthDate { get; set; }
    /// <summary>"Patient" | "Admin" | "Doctor"</summary>
    public string Role { get; set; } = "Patient";
    /// <summary>Solo requerido cuando Role = "Doctor"</summary>
    public DoctorSpecialty? DoctorType { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserInput
{
    public string UserId { get; set; } = "";
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? IdentificationNumber { get; set; }
    public DocumentType? DocumentType { get; set; }
    public GenderType? Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Role { get; set; }
    public DoctorSpecialty? DoctorType { get; set; }
}

public class UserFilterInput
{
    public string? SearchText { get; set; }
    /// <summary>"Patient" | "Admin" | "Doctor" | null para todos</summary>
    public string? RoleFilter { get; set; }
    /// <summary>"Active" | "Inactive" | null para todos</summary>
    public string? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
