using PiedraAzul.Application.Common.Models.Doctor;

namespace PiedraAzul.GraphQL.Types;

public enum DoctorSpecialty { NaturalMedicine, Chiropractic, Optometry, Physiotherapy }

public class DoctorType
{
    public string DoctorId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Name { get; set; } = "";
    public DoctorSpecialty Specialty { get; set; }
    public string LicenseNumber { get; set; } = "";
    public string? Notes { get; set; }
    public string? AvatarUrl { get; set; }

    public static DoctorType FromDto(DoctorDto d) => new()
    {
        DoctorId = d.Id,
        UserId = d.Id,
        Name = d.Name,
        Specialty = (DoctorSpecialty)d.Specialty,
        LicenseNumber = d.LicenseNumber,
        Notes = d.Notes,
        AvatarUrl = d.AvatarUrl
    };
}
