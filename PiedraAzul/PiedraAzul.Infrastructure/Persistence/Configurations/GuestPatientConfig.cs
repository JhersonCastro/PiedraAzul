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
        }
    }
}