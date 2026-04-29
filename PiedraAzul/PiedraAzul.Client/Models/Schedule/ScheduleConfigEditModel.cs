using System.ComponentModel.DataAnnotations;

namespace PiedraAzul.Client.Models.Schedule;

public class ScheduleConfigEditModel : ScheduleConfigModel, IValidatableObject
{
    [Required(ErrorMessage = "Selecciona un especialista.")]
    public new string DoctorId
    {
        get => base.DoctorId;
        set => base.DoctorId = value;
    }

    [Range(1, 52, ErrorMessage = "La ventana de semanas debe ser mayor a 0.")]
    public new int BookingWindowWeeks
    {
        get => base.BookingWindowWeeks;
        set => base.BookingWindowWeeks = value;
    }

    [Range(5, 60, ErrorMessage = "El intervalo debe estar entre 5 y 60 minutos.")]
    public new int IntervalMinutes
    {
        get => base.IntervalMinutes;
        set => base.IntervalMinutes = value;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Availability is null || Availability.Count == 0)
        {
            yield return new ValidationResult("Debes configurar disponibilidad para al menos un día.", [nameof(Availability)]);
            yield break;
        }

        if (!Availability.Any(day => day.IsEnabled))
        {
            yield return new ValidationResult("Debes habilitar al menos un día.", [nameof(Availability)]);
        }

        foreach (var day in Availability.Where(day => day.IsEnabled))
        {
            if (day.StartTime >= day.EndTime)
            {
                yield return new ValidationResult("La hora de inicio debe ser menor a la hora de fin.", [nameof(Availability)]);
                yield break;
            }
        }
    }
}
