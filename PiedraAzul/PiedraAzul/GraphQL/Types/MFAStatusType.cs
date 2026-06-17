using HotChocolate.Types;

namespace PiedraAzul.GraphQL.Types;

[ObjectType("MFAStatus")]
public class MFAStatusType
{
    public bool EmailOTPEnabled { get; set; }
    public bool TOTPEnabled { get; set; }
    public bool HasBackupCodes { get; set; }
}
