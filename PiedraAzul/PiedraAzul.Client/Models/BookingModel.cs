using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models
{
    public class BookingModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MinLength(3, ErrorMessage = "El nombre es muy corto")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        public string Direccion { get; set; }
        [Required(ErrorMessage = "El doctor es obligatorio")]
        public string DoctorId { get; set; }
    }

}
