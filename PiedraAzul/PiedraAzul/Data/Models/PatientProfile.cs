using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class PatientProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PatientId { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string ExtraContactInfo { get; set; } = string.Empty;

    }
}
