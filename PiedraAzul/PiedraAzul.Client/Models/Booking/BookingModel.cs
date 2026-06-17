using PiedraAzul.Client.Models.UserProfiles;
using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.Booking
{
    public class BookingModel
    {
        //Selected Patient
        [Required]
        [MinLength(5, ErrorMessage = "El ID debe tener al menos 5 caracteres")]
        public string? PatientIdentification { get; set; }
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MinLength(3, ErrorMessage = "El nombre es muy corto")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        public string? PatientPhone { get; set; }


        public string? PatientAddress { get; set; }

        // ── OTP Verification ─────────────────────────────────────────
        /// <summary>Canal elegido por el huésped: "whatsapp" o "email"</summary>
        public string OtpChannel { get; set; } = "email";

        /// <summary>Email solo requerido si OtpChannel == "email"</summary>
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? PatientEmail { get; set; }

        /// <summary>Token opaco devuelto por sendGuestOtp</summary>
        public string? OtpSessionToken { get; set; }

        /// <summary>Código de 6 dígitos ingresado por el usuario</summary>
        public string? OtpCode { get; set; }

        /// <summary>true cuando el OTP fue verificado correctamente</summary>
        public bool OtpVerified { get; set; }
        // ─────────────────────────────────────────────────────────────

        [Required(ErrorMessage = "El doctor es obligatorio")]
        public string? DoctorId { get; set; }

        public DoctorModel? Doctor { get; set; }

        [Required(ErrorMessage = "Por favor selecciona una horario para la cita")]
        public string? SlotId { get; set; }

        public AppointmentSchedulerModel? AppointmentSchedulerModel { get; set; }

        public DateTime DayOfYear { get; set; }
    }

}
