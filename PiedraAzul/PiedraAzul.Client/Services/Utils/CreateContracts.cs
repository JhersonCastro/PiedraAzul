using PiedraAzul.Client.Services.GraphQLServices;

namespace PiedraAzul.Client.Services.Utils;

public static class CreateContracts
{
    public static GuestPatientGqlInput CreateGuestPatientInput(
        string patientName,
        string patientPhone,
        string patientIdentification,
        string extraInfo,
        string? email = null,
        string documentType = "CC")
    {
        return new GuestPatientGqlInput(
            Identification: patientIdentification,
            Name: patientName,
            Phone: patientPhone,
            ExtraInfo: extraInfo,
            Email: email,
            DocumentType: documentType
        );
    }
}
