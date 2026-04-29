namespace PiedraAzul.GraphQL.Types;

public class MFARequiredType
{
    public required string MFAToken { get; set; }
    public required string MFAMethod { get; set; }
    public required bool HasEmail { get; set; }
}
