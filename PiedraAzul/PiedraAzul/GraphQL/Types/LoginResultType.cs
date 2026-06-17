namespace PiedraAzul.GraphQL.Types;

public class LoginResultType
{
    public UserType? User { get; set; }
    public MFARequiredType? MFARequired { get; set; }
}
