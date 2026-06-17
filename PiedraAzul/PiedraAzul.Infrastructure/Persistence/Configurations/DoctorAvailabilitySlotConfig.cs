using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Doctor;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class DoctorAvailabilitySlotConfig : IEntityTypeConfiguration<DoctorAvailabilitySlot>
    {
        public void Configure(EntityTypeBuilder<DoctorAvailabilitySlot> builder)
        {
            builder.ToTable("DoctorAvailabilitySlots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DoctorId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.DayOfWeek)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.StartTime)
                .IsRequired();

            builder.Property(x => x.EndTime)
                .IsRequired();

            builder.HasIndex(x => new { x.DoctorId, x.DayOfWeek });
        }
    }
}