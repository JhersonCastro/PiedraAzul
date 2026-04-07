using Shared.Grpc;

namespace PiedraAzul.Client.Models.UserProfiles
{
    public class PatientModel
    {
        public string Id { get; set; }
        public string PatientIdentification { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }

        public PatientType Type { get; set; } 

        public bool IsRegistered => Type == PatientType.Registered;
        public bool IsGuest => Type == PatientType.Guest;
    }
}