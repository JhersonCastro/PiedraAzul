using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Config;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class SystemConfigConfig : IEntityTypeConfiguration<SystemConfig>
    {
        public void Configure(EntityTypeBuilder<SystemConfig> builder)
        {
            builder.ToTable("SystemConfigs");

            builder.Property<int>("Id")
                .ValueGeneratedOnAdd();

            builder.HasKey("Id");

            builder.Property(x => x.BookingWindowWeeks)
                .IsRequired();
        }
    }
}