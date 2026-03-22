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
        [Required(ErrorMessage = "El doctor es obligatorio")]
        public string? DoctorId { get; set; }

        public DoctorModel? Doctor { get; set; }

        [Required(ErrorMessage = "Por favor selecciona una horario para la cita")]
        public string? SlotId { get; set; }

        public AppointmentSchedulerModel? AppointmentSchedulerModel { get; set; }

        public DateTime DayOfYear { get; set; }
    }

}
