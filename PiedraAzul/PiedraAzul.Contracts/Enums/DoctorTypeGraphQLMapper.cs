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
}