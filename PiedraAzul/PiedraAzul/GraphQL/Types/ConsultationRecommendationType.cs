using PiedraAzul.Application.Common.Models.Consultation;

namespace PiedraAzul.GraphQL.Types;

public class ConsultationRecommendationType
{
    public string RecommendedSpecialty { get; set; } = "";
    public string SpecialtyLabel { get; set; } = "";
    public string Reasoning { get; set; } = "";
    public int ConfidencePercent { get; set; }

    public static ConsultationRecommendationType From(ConsultationRecommendation r) => new()
    {
        RecommendedSpecialty = r.RecommendedSpecialty,
        SpecialtyLabel = r.SpecialtyLabel,
        Reasoning = r.Reasoning,
        ConfidencePercent = r.ConfidencePercent
    };
}
