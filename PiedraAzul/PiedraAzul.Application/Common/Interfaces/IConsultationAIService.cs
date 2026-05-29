using PiedraAzul.Application.Common.Models.Consultation;

namespace PiedraAzul.Application.Common.Interfaces;

/// <summary>
/// Servicio que analiza los síntomas del paciente con IA y recomienda un especialista.
/// La implementación se comunica con un proveedor de IA externo (Google Gemini).
/// </summary>
public interface IConsultationAIService
{
    Task<ConsultationRecommendation> RecommendSpecialtyAsync(
        ConsultationRequest request,
        CancellationToken cancellationToken = default);
}
