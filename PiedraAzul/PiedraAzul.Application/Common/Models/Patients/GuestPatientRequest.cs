using PiedraAzul.Domain.Entities.Shared.Enums;

namespace PiedraAzul.Application.Common.Models.Patients
{
    public class GuestPatientRequest
    {
        public string Identification { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string? ExtraInfo { get; set; }
        public string? Email { get; set; }
        public DocumentType DocumentType { get; set; } = DocumentType.CC;
    }
}
