using PiedraAzul.Application.Common.Models.Appointments;

namespace PiedraAzul.GraphQL.Types;

public class AppointmentType
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

    public static AppointmentType FromDto(AppointmentDto a) => new()
    {
        Id = a.Id.ToString(),
        PatientUserId = a.PatientUserId,
        PatientGuestId = a.PatientGuestId,
        PatientType = a.PatientType,
        PatientName = a.PatientName,
        AppointmentSlotId = a.SlotId.ToString(),
        DoctorId = a.DoctorId,
        DoctorName = a.DoctorName,
        Specialty = a.Specialty,
        Start = a.Start,
        CreatedAt = a.CreatedAt,
        Status = a.Status
    };

    public static AppointmentType FromDomain(Domain.Entities.Operations.Appointment a) => new()
    {
        Id = a.Id.ToString(),
        PatientUserId = a.PatientUserId,
        PatientGuestId = a.PatientGuestId,
        PatientType = a.PatientUserId != null ? "Registered" : "Guest",
        PatientName = "",
        AppointmentSlotId = a.DoctorAvailabilitySlotId.ToString(),
        Start = a.Date.ToDateTime(TimeOnly.MinValue),
        CreatedAt = a.CreatedAt,
        Status = a.Status.ToString()
    };
}
