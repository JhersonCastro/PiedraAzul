namespace PiedraAzul.Client.Models.GraphQL;

public class AdminUserGQL
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string IdentificationNumber { get; set; } = "";
    public string DocumentType { get; set; } = "CC";
    public string Gender { get; set; } = "NON_SPECIFIED";
    public DateTime? BirthDate { get; set; }
    public string AvatarUrl { get; set; } = "default.png";
    public List<string> Roles { get; set; } = new();
    public string? DoctorType { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string? TempPassword { get; set; }

    public string PrimaryRole => Roles.FirstOrDefault() ?? "Patient";

    public string DoctorTypeLabel => DoctorType switch
    {
        "NATURAL_MEDICINE" => "Medicina Natural",
        "CHIROPRACTIC"     => "Quiropráctica",
        "OPTOMETRY"        => "Optometría",
        "PHYSIOTHERAPY"    => "Fisioterapia",
        _                  => ""
    };
}

public class UserListResultGQL
{
    public List<AdminUserGQL> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
