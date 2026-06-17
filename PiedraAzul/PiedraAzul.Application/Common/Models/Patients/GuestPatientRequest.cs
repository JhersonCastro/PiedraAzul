namespace PiedraAzul.Application.Common.Models.Patients
{
    public class GuestPatientRequest
    {
        public string Identification { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string? ExtraInfo { get; set; }
    }
}