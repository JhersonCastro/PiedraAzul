namespace PiedraAzul.Client.Models.GraphQL;

public class SlotGQL
{
    public string Id { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAvailable { get; set; }
}
