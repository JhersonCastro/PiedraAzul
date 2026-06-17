using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Infrastructure.Auth;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfig : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHashed)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.IsRevoked)
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasIndex(x => x.TokenHashed)
                .IsUnique();

            builder.HasIndex(x => x.UserId);
        }
    }
}