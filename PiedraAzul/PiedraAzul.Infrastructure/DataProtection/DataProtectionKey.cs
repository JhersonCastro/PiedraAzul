namespace PiedraAzul.Infrastructure.DataProtection;

public class DataProtectionKey
{
    public int Id { get; set; }
    public string? FriendlyName { get; set; }
    public string Xml { get; set; } = default!;
}
