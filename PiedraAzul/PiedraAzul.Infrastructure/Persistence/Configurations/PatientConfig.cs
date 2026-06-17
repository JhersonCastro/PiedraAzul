using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class PatientConfig : IEntityTypeConfiguration<Patient>
    {
        public void Configure(EntityTypeBuilder<Patient> builder)
        {
            builder.ToTable("Patients");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.HasDiscriminator<string>("PatientType")
                .HasValue<RegisteredPatient>("Registered")
                .HasValue<GuestPatient>("Guest");
        }
    }
}