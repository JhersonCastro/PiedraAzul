using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Data.Models
{
    public class PatientGuest
    {
        [Key]
        public string PatientIdentification { get; set; }
        
        public string PatientName { get; set; }
        public string PatientPhone { get; set; }
        public string PatientExtraInfo { get; set; }
    }
}
