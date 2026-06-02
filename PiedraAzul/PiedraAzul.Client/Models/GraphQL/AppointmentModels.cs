namespace PiedraAzul.Client.Models.GraphQL;

public class AppointmentGQL
{
    public string Id { get; set; } = "";
    public string? PatientUserId { get; set; }
    public string? PatientGuestId { get; set; }
    public string PatientType { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string AppointmentSlotId { get; set; } = "";
    public string DoctorId { get; set; } = "";
    public string DoctorName { get; set; } = "";
    public string Specialty { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Active";

    /// <summary>True si esta cita proviene de un reagendamiento anterior.</summary>
    public bool WasRescheduled { get; set; }

    /// <summary>Historial cronológico del linaje de reagendamientos.</summary>
    public List<AppointmentRescheduleEntryGQL> RescheduleHistory { get; set; } = new();
}

public class AppointmentRescheduleEntryGQL
{
    public string RescheduledByUserId { get; set; } = "";
    public string RescheduledByName { get; set; } = "";
    public string[] RescheduledByRoles { get; set; } = Array.Empty<string>();

    public DateTime RescheduledAt { get; set; }

    public DateTime OriginalDate { get; set; }
    public DateTime NewDate { get; set; }

    public string OriginalDoctorId { get; set; } = "";
    public string OriginalDoctorName { get; set; } = "";
    public string NewDoctorId { get; set; } = "";
    public string NewDoctorName { get; set; } = "";
}
