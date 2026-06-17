namespace PiedraAzul.GraphQL.Types;

public class SlotType
{
    public string Id { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
}
