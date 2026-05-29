namespace PiedraAzul.Application.Common.Models.Consultation;

/// <summary>
/// Datos que el paciente entrega en la consulta inteligente (anónima).
/// Las primeras tres son respuestas tipo ABCD; <see cref="Description"/> es texto libre.
/// </summary>
public record ConsultationRequest(
    string SymptomArea,
    string Duration,
    string Intensity,
    string Description
);

/// <summary>
/// Recomendación generada por la IA: a qué especialista derivar al paciente.
/// </summary>
public record ConsultationRecommendation(
    string RecommendedSpecialty,  // Código: NATURAL_MEDICINE, CHIROPRACTIC, OPTOMETRY, PHYSIOTHERAPY
    string SpecialtyLabel,        // Etiqueta en español: Terapia Natural, Quiropraxia, etc.
    string Reasoning,             // Explicación breve y cálida para el paciente
    int ConfidencePercent         // Confianza 0-100
);
