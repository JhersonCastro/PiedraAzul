using PiedraAzul.Application.Common.Models.Appointments;

namespace PiedraAzul.GraphQL.Types;

public class AppointmentRescheduleEntryType
{
    public string RescheduledByUserId { get; set; } = "";
    public string RescheduledByName { get; set; } = "";
    public string[] RescheduledByRoles { get; set; } = [];

    public DateTime RescheduledAt { get; set; }

    public DateTime OriginalDate { get; set; }
    public DateTime NewDate { get; set; }

    public string OriginalDoctorId { get; set; } = "";
    public string OriginalDoctorName { get; set; } = "";
    public string NewDoctorId { get; set; } = "";
    public string NewDoctorName { get; set; } = "";

    public static AppointmentRescheduleEntryType FromDto(AppointmentRescheduleEntryDto e) => new()
    {
        RescheduledByUserId = e.RescheduledByUserId,
        RescheduledByName = e.RescheduledByName,
        RescheduledByRoles = e.RescheduledByRoles,
        RescheduledAt = e.RescheduledAt,
        OriginalDate = e.OriginalDate,
        NewDate = e.NewDate,
        OriginalDoctorId = e.OriginalDoctorId,
        OriginalDoctorName = e.OriginalDoctorName,
        NewDoctorId = e.NewDoctorId,
        NewDoctorName = e.NewDoctorName
    };
}
