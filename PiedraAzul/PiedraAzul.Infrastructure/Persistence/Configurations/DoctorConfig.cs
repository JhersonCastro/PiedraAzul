using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Doctor;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class DoctorConfig : IEntityTypeConfiguration<Doctor>
    {
        public void Configure(EntityTypeBuilder<Doctor> builder)
        {
            builder.ToTable("Doctors");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(x => x.Specialty)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.LicenseNumber)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasMany(x => x.Slots)
                .WithOne()
                .HasForeignKey("DoctorId")
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(x => x.Slots)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}