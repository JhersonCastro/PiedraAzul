using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        //Optional, as an appointment might be created by a guest patient, maybe we never create a account
        public Guid? PatientId { get; set; }
        public PatientProfile? Patient { get; set; }

        //Patient Basic data if the patient is a guest, this is to avoid having to create a account for them, and also to have a record of their data at the time of the appointment
        public string? PatientIdentificationNumber { get; set; }
        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
        public string? PatientExtraInfo { get; set; }


        public Guid DoctorId { get; set; }
        public DoctorProfile Doctor { get; set; }

        public Guid DoctorAvailabilitySlotId { get; set; }
        public DoctorAvailabilitySlot DoctorAvailabilitySlot { get; set; }

        public DateTime Date { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
