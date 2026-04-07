namespace PiedraAzul.Infrastructure.Persistence.Auth
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }

        public string TokenHashed { get; private set; } = null!;

        public DateTime ExpiresAt { get; private set; }

        public bool IsRevoked { get; private set; }

        public string UserId { get; private set; } = null!;

        private RefreshToken() { }

        public RefreshToken(string tokenHashed, DateTime expiresAt, string userId)
        {
            Id = Guid.NewGuid();
            TokenHashed = tokenHashed;
            ExpiresAt = expiresAt;
            UserId = userId;
            IsRevoked = false;
        }

        public void Revoke()
        {
            IsRevoked = true;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow >= ExpiresAt;
        }
    }
}