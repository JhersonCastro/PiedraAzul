using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Profiles.Patients;

namespace PiedraAzul.Infrastructure.Persistence.Configurations;

public class GuestVerificationSessionConfig : IEntityTypeConfiguration<GuestVerificationSession>
{
    public void Configure(EntityTypeBuilder<GuestVerificationSession> builder)
    {
        builder.HasKey(x => x.Hash);

        builder.Property(x => x.Hash).HasMaxLength(64);
        builder.Property(x => x.SessionType).HasConversion<int>().IsRequired();

        // GuestId ahora es opcional (FLUJO 2 no tiene guest)
        builder.Property(x => x.GuestId).HasMaxLength(30).IsRequired(false);

        // Datos de usuario registrado (FLUJO 2)
        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired(false);
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired(false);
        builder.Property(x => x.UserPhone).HasMaxLength(30).IsRequired(false);
        builder.Property(x => x.UserEmail).HasMaxLength(200).IsRequired(false);

        builder.Property(x => x.OtpCode).HasMaxLength(10);
        builder.Property(x => x.Channel).HasMaxLength(20);

        builder.HasIndex(x => x.GuestId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne(x => x.Guest)
            .WithMany()
            .HasForeignKey(x => x.GuestId)
            .HasPrincipalKey(g => g.Id)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}
