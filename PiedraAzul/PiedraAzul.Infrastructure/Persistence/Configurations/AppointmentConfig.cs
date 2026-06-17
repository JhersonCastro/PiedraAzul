using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Operations;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class AppointmentConfig : IEntityTypeConfiguration<Appointment>
    {
        public void Configure(EntityTypeBuilder<Appointment> builder)
        {
            builder.ToTable("Appointments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DoctorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.DoctorAvailabilitySlotId)
                .IsRequired();

            builder.Property(x => x.PatientUserId)
                .HasMaxLength(450);

            builder.Property(x => x.PatientGuestId)
                .HasMaxLength(450);

            builder.Property(x => x.Date)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Slot)
                .WithMany()
                .HasForeignKey(x => x.DoctorAvailabilitySlotId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.DoctorId, x.Date });
            builder.HasIndex(x => x.DoctorAvailabilitySlotId);
        }
    }
}