namespace PiedraAzul.GraphQL.Types;

public enum PatientTypeEnum { Unknown, Registered, Guest }

public class PatientSearchResultType
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Identification { get; set; } = "";
    public string Phone { get; set; } = "";
    public PatientTypeEnum Type { get; set; }
}
