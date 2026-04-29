using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Config;

namespace PiedraAzul.Infrastructure.Persistence.Configurations;

public class SystemConfigConfig : IEntityTypeConfiguration<SystemConfig>
{
    public void Configure(EntityTypeBuilder<SystemConfig> builder)
    {
        builder.ToTable("SystemConfigs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BookingWindowWeeks)
            .IsRequired();
    }
}
