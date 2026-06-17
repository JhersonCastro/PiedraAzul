namespace PiedraAzul.Client.Models.GraphQL;

public class DoctorGQL
{
    public string DoctorId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Specialty { get; set; } = "";
    public string? AvatarUrl { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Notes { get; set; }
}
