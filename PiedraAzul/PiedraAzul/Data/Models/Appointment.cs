using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid PatientId { get; set; }
        public PatientProfile Patient { get; set; }


        public Guid DoctorId { get; set; }
        public DoctorProfile Doctor { get; set; }

        public Guid DoctorAvailabilityBlockId { get; set; }
        public DoctorAvailabilitySlot DoctorAvailabilityBlock { get; set; }

        public DateTime DayOfYear { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
