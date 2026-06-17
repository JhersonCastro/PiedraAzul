using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLAvailabilityService(GraphQLHttpClient client)
{
    public async Task<Result<List<SlotGQL>>> GetDoctorSlotsByDate(
        string doctorId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        const string query = """
            query GetAvailableSlots($doctorId: String!, $date: DateTime!) {
                availableSlots(doctorId: $doctorId, date: $date) {
                    id start end isAvailable
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<SlotGQL>>(
                query,
                new { doctorId, date = date.ToUniversalTime().ToString("o") },
                "availableSlots");
            return result ?? new();
        });
    }
}
