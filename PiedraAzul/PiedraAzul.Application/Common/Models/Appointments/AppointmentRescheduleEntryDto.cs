namespace PiedraAzul.Application.Common.Models.Appointments
{
    /// <summary>Un evento de reagendamiento dentro del historial de una cita.</summary>
    public class AppointmentRescheduleEntryDto
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
    }
}
