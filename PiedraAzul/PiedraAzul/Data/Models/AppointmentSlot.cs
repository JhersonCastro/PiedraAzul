using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class AppointmentSlot
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid DoctorId { get; set; }
        public DoctorProfile Doctor { get; set; }

        public Guid DoctorAvailabilityBlockId { get; set; }
        public DoctorAvailabilityBlock DoctorAvailabilityBlock { get; set; }

        public DateTime DayOfYear { get; set; }


    }
}
