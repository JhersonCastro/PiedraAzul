using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class AppointmentRescheduleRecordConfig : IEntityTypeConfiguration<AppointmentRescheduleRecord>
    {
        public void Configure(EntityTypeBuilder<AppointmentRescheduleRecord> builder)
        {
            builder.ToTable("AppointmentRescheduleRecords");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RootAppointmentId)
                .IsRequired();

            builder.Property(x => x.OriginalAppointmentId)
                .IsRequired();

            builder.Property(x => x.NewAppointmentId)
                .IsRequired();

            builder.Property(x => x.RescheduledByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.RescheduledAt)
                .IsRequired();

            builder.Property(x => x.OriginalDate)
                .IsRequired();

            builder.Property(x => x.NewDate)
                .IsRequired();

            builder.Property(x => x.OriginalDoctorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.NewDoctorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(x => x.RootAppointmentId);
            builder.HasIndex(x => x.NewAppointmentId);
            builder.HasIndex(x => x.OriginalAppointmentId);
        }
    }
}
