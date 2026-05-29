using PiedraAzul.Contracts.Enums;

public static class DoctorTypeGraphQLMapper
{
    public static string ToGraphQL(this DoctorType type)
    {
        return type switch
        {
            DoctorType.NaturalMedicine => "NATURAL_MEDICINE",
            DoctorType.Chiropractic => "CHIROPRACTIC",
            DoctorType.Optometry => "OPTOMETRY",
            DoctorType.Physiotherapy => "PHYSIOTHERAPY",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>
    /// Convierte un código GraphQL (NATURAL_MEDICINE, etc.) al enum.
    /// Devuelve null si el código no es válido.
    /// </summary>
    public static DoctorType? FromGraphQL(string? code)
    {
        return code?.Trim().ToUpperInvariant() switch
        {
            "NATURAL_MEDICINE" => DoctorType.NaturalMedicine,
            "CHIROPRACTIC" => DoctorType.Chiropractic,
            "OPTOMETRY" => DoctorType.Optometry,
            "PHYSIOTHERAPY" => DoctorType.Physiotherapy,
            _ => null
        };
    }
}