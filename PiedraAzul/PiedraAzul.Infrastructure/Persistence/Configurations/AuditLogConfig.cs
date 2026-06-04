using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Audit;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(450);
            builder.Property(x => x.Action).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Source).HasMaxLength(20).IsRequired();

            builder.Property(x => x.ActorUserId).HasMaxLength(450);
            builder.Property(x => x.ActorName).HasMaxLength(200);
            builder.Property(x => x.ActorRoles).HasMaxLength(200);
            builder.Property(x => x.IpAddress).HasMaxLength(64);

            builder.Property(x => x.SubjectIdentification).HasMaxLength(50);
            builder.Property(x => x.SubjectName).HasMaxLength(200);
            builder.Property(x => x.SubjectPhone).HasMaxLength(30);

            builder.Property(x => x.Data).HasColumnType("jsonb");

            builder.HasIndex(x => x.Timestamp);
            builder.HasIndex(x => x.EntityType);
            builder.HasIndex(x => x.Action);
            builder.HasIndex(x => x.SubjectIdentification);
            builder.HasIndex(x => x.ActorUserId);
        }
    }
}
