using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PiedraAzul.Data.Models
{
    public class Appointment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        // Usuario paciente (opcional por invitados)
        public string? PatientUserId { get; set; }
        public ApplicationUser? Patient { get; set; }

        // Invitado (si no hay cuenta)
        public string? PatientGuestId { get; set; }
        public PatientGuest? PatientGuest { get; set; }

        // Doctor SIEMPRE es usuario
        [Required]
        public string DoctorUserId { get; set; }
        public ApplicationUser Doctor { get; set; }

        public Guid DoctorAvailabilitySlotId { get; set; }
        public DoctorAvailabilitySlot DoctorAvailabilitySlot { get; set; }

        public DateTime Date { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}