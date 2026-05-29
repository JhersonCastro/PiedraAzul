using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.Consultation;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.GraphQLServices;

public record ConsultationGqlInput(
    string SymptomArea,
    string Duration,
    string Intensity,
    string Description
);

public class ConsultationService(GraphQLHttpClient client)
{
    /// <summary>
    /// Envía las respuestas de la consulta y obtiene la recomendación de especialista.
    /// </summary>
    public async Task<Result<ConsultationRecommendation>> ProcessAsync(ConsultationGqlInput input)
    {
        const string mutation = """
            mutation ProcessConsultation($input: ConsultationInput!) {
                processConsultation(input: $input) {
                    recommendedSpecialty
                    specialtyLabel
                    reasoning
                    confidencePercent
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<ConsultationRecommendation>(
                mutation,
                new { input },
                "processConsultation");
            return result!;
        });
    }
}
