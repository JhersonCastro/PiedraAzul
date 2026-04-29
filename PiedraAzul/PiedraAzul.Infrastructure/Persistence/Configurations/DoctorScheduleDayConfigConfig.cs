using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Config;

namespace PiedraAzul.Infrastructure.Persistence.Configurations;

public class DoctorScheduleDayConfigConfig : IEntityTypeConfiguration<DoctorScheduleDayConfig>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleDayConfig> builder)
    {
        builder.ToTable("DoctorScheduleDayConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.HasIndex(x => new { x.DoctorScheduleConfigId, x.DayOfWeek })
            .IsUnique();
    }
}
