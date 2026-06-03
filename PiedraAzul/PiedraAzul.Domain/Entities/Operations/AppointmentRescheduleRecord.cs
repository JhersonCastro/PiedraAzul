namespace PiedraAzul.Domain.Entities.Operations
{
    /// <summary>
    /// Registro de auditoría de un evento de reagendamiento: la cita <see cref="OriginalAppointmentId"/>
    /// pasó a ser <see cref="NewAppointmentId"/>. <see cref="RootAppointmentId"/> identifica el linaje
    /// completo (la cita original que inició la cadena) para poder traer todo el historial en una sola query.
    /// </summary>
    public class AppointmentRescheduleRecord
    {
        public Guid Id { get; private set; }

        /// <summary>Cita original que inició la cadena de reagendamientos (clave de linaje).</summary>
        public Guid RootAppointmentId { get; private set; }

        /// <summary>Cita que fue reagendada (quedó con Status = Rescheduled).</summary>
        public Guid OriginalAppointmentId { get; private set; }

        /// <summary>Cita nueva creada a partir del reagendamiento.</summary>
        public Guid NewAppointmentId { get; private set; }

        /// <summary>Id (nameidentifier) de quien reagendó. Nombre y roles se resuelven en lectura.</summary>
        public string RescheduledByUserId { get; private set; }

        /// <summary>Momento (UTC) en que se reagendó.</summary>
        public DateTime RescheduledAt { get; private set; }

        public DateOnly OriginalDate { get; private set; }
        public DateOnly NewDate { get; private set; }

        public string OriginalDoctorId { get; private set; }
        public string NewDoctorId { get; private set; }

        private AppointmentRescheduleRecord() { }

        private AppointmentRescheduleRecord(
            Guid rootAppointmentId,
            Guid originalAppointmentId,
            Guid newAppointmentId,
            string rescheduledByUserId,
            DateTime rescheduledAt,
            DateOnly originalDate,
            DateOnly newDate,
            string originalDoctorId,
            string newDoctorId)
        {
            Id = Guid.NewGuid();
            RootAppointmentId = rootAppointmentId;
            OriginalAppointmentId = originalAppointmentId;
            NewAppointmentId = newAppointmentId;
            RescheduledByUserId = rescheduledByUserId;
            RescheduledAt = rescheduledAt;
            OriginalDate = originalDate;
            NewDate = newDate;
            OriginalDoctorId = originalDoctorId;
            NewDoctorId = newDoctorId;
        }

        public static AppointmentRescheduleRecord Create(
            Guid rootAppointmentId,
            Guid originalAppointmentId,
            Guid newAppointmentId,
            string rescheduledByUserId,
            DateOnly originalDate,
            DateOnly newDate,
            string originalDoctorId,
            string newDoctorId)
        {
            return new AppointmentRescheduleRecord(
                rootAppointmentId,
                originalAppointmentId,
                newAppointmentId,
                rescheduledByUserId,
                DateTime.UtcNow,
                originalDate,
                newDate,
                originalDoctorId,
                newDoctorId);
        }
    }
}
