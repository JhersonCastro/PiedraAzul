using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Models.UserProfiles;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PiedraAzul.Client.Models.Booking
{
    public class BookingModel
    {
        // ── Identificación ───────────────────────────────────────────
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

        // ── OTP Verification (FLUJO 1) ────────────────────────────────
        /// <summary>Canal elegido por el huésped: "sms", "whatsapp" o "email"</summary>
        public string OtpChannel { get; set; } = "sms";

        /// <summary>Email solo requerido si OtpChannel == "email"</summary>
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string? PatientEmail { get; set; }

        /// <summary>Token opaco devuelto por sendGuestOtp</summary>
        public string? OtpSessionToken { get; set; }

        /// <summary>Código de 6 dígitos ingresado por el usuario</summary>
        public string? OtpCode { get; set; }

        /// <summary>true cuando el OTP fue verificado correctamente</summary>
        public bool OtpVerified { get; set; }

        // ── Doctor & Slot ─────────────────────────────────────────────
        [Required(ErrorMessage = "El doctor es obligatorio")]
        public string? DoctorId { get; set; }

        public DoctorModel? Doctor { get; set; }

        [Required(ErrorMessage = "Por favor selecciona una horario para la cita")]
        public string? SlotId { get; set; }

        public AppointmentSchedulerModel? AppointmentSchedulerModel { get; set; }

        public DateTime DayOfYear { get; set; }

        // ── Patient Search State ──────────────────────────────────────
        [JsonIgnore]
        public PatientSearchResultGQL? SearchResult { get; set; }

        /// <summary>"REGISTERED" | "GUEST" | "NOT_FOUND" | "NO_CONTACT" | "ERROR" | null</summary>
        [JsonIgnore]
        public string? SearchResultType { get; set; }

        [JsonIgnore]
        public bool PatientDataFromSearch { get; set; }

        /// <summary>true = FLUJO 1 (nuevo usuario, completar datos en Step 1)</summary>
        [JsonIgnore]
        public bool IsNewPatient { get; set; }

        // ── Pre-verificación (FLUJO 2 y FLUJO 3) ─────────────────────
        /// <summary>Hash de la sesión de verificación devuelto por lookupGuestByIdentification.</summary>
        [JsonIgnore]
        public string? VerificationHash { get; set; }

        /// <summary>true = el usuario fue pre-verificado al inicio; omitir OTP final.</summary>
        [JsonIgnore]
        public bool IsPreVerifiedGuest { get; set; }

        /// <summary>true = FLUJO 2 (usuario registrado continuando como invitado, datos READONLY)</summary>
        [JsonIgnore]
        public bool IsRegisteredContinuingAsGuest { get; set; }

        // ── Datos enmascarados disponibles (para modal de validación) ─
        [JsonIgnore]
        public bool HasPhoneAvailable { get; set; }

        [JsonIgnore]
        public bool HasEmailAvailable { get; set; }

        [JsonIgnore]
        public string? MaskedPhone { get; set; }

        [JsonIgnore]
        public string? MaskedEmail { get; set; }

        [JsonIgnore]
        public bool ChannelValidationVerified { get; set; }
    }
}
