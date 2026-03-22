using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.UserProfiles
{
    public class PatientModel
    {
        public string Id { get; set; }
        public string PatientIdentification { get; set; }
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
    }
}
