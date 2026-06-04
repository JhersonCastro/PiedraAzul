using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class GuestPatientConfig : IEntityTypeConfiguration<GuestPatient>
    {
        public void Configure(EntityTypeBuilder<GuestPatient> builder)
        {
            builder.Property(x => x.Phone)
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(x => x.ExtraInfo)
                .HasMaxLength(500);

            builder.Property(x => x.Email)
                .HasMaxLength(200);

            // Merge guest → cuenta registrada (auditoría + evita re-merge)
            builder.Property(x => x.MergedToUserId)
                .HasMaxLength(450);

            builder.Property(x => x.MergedAt);
        }
    }
}