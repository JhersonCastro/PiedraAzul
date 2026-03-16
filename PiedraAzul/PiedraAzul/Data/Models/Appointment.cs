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

        public Guid AppointmentSlotId { get; set; }
        public AppointmentSlot AppointmentSlot { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
