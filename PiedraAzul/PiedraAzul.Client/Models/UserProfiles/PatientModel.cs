namespace PiedraAzul.Client.Models.UserProfiles;

public enum PatientTypeClient { Unknown = 0, Registered = 1, Guest = 2 }

public class PatientModel
{
    public string Id { get; set; } = "";
    public string PatientIdentification { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string PatientPhone { get; set; } = "";
    public PatientTypeClient Type { get; set; }

    public bool IsRegistered => Type == PatientTypeClient.Registered;
    public bool IsGuest => Type == PatientTypeClient.Guest;
}
