using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Infrastructure.Auth;

namespace PiedraAzul.Infrastructure.Persistence.Configurations;

public class PasskeyCredentialConfig : IEntityTypeConfiguration<PasskeyCredential>
{
    public void Configure(EntityTypeBuilder<PasskeyCredential> builder)
    {
        builder.ToTable("PasskeyCredentials");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.CredentialId).IsRequired();
        builder.Property(x => x.PublicKey).IsRequired();
        builder.Property(x => x.FriendlyName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SignatureCounter).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
    }
}
