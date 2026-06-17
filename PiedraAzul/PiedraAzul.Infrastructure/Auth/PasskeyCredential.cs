namespace PiedraAzul.Infrastructure.Auth;

public class PasskeyCredential
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public byte[] CredentialId { get; private set; } = null!;
    public byte[] PublicKey { get; private set; } = null!;
    public uint SignatureCounter { get; private set; }
    public string FriendlyName { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private PasskeyCredential() { }

    public PasskeyCredential(string userId, byte[] credentialId, byte[] publicKey, uint signatureCounter, string friendlyName)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignatureCounter = signatureCounter;
        FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? "Passkey" : friendlyName;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateCounter(uint newCounter)
    {
        SignatureCounter = newCounter;
    }
}
