using HotChocolate;
using PiedraAzul.Application.Common.Interfaces;
using PiedraAzul.Application.Common.Models.Consultation;
using PiedraAzul.GraphQL.Inputs;
using PiedraAzul.GraphQL.Types;

namespace PiedraAzul.GraphQL;

public partial class Mutation
{
    /// <summary>
    /// Procesa una consulta de síntomas (anónima, sin autenticación) y recomienda
    /// un especialista usando IA. No realiza diagnóstico médico.
    /// </summary>
    public async Task<ConsultationRecommendationType> ProcessConsultationAsync(
        ConsultationInput input,
        [Service] IConsultationAIService consultationService)
    {
        if (string.IsNullOrWhiteSpace(input.Description) && string.IsNullOrWhiteSpace(input.SymptomArea))
            throw new GraphQLException("Cuéntanos qué te ocurre para poder ayudarte.");

        var recommendation = await consultationService.RecommendSpecialtyAsync(
            new ConsultationRequest(
                input.SymptomArea ?? "",
                input.Duration ?? "",
                input.Intensity ?? "",
                input.Description ?? ""));

        return ConsultationRecommendationType.From(recommendation);
    }
}
