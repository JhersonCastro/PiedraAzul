namespace PiedraAzul.Client.Models.GraphQL;

public class AppointmentGQL
{
    public string Id { get; set; } = "";
    public string? PatientUserId { get; set; }
    public string? PatientGuestId { get; set; }
    public string PatientType { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string AppointmentSlotId { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime CreatedAt { get; set; }
}
