using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class RegisteredPatientConfig : IEntityTypeConfiguration<RegisteredPatient>
    {
        public void Configure(EntityTypeBuilder<RegisteredPatient> builder)
        {
            builder.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();
        }
    }
}