using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLAvailabilityService(GraphQLHttpClient client)
{
    /// <summary>
    /// Obtiene la ventana de reserva personalizada de un doctor (en semanas).
    /// </summary>
    public async Task<Result<int>> GetBookingWindowWeeksAsync(string doctorId)
    {
        const string query = """
            query GetBookingWindowWeeks($doctorId: String!) {
                bookingWindowWeeks(doctorId: $doctorId)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<int>(
                query,
                new { doctorId },
                "bookingWindowWeeks");
            return result > 0 ? result : 4;
        });
    }

    public async Task<Result<List<DateTime>>> GetDoctorAvailableDaysAsync(
        string doctorId,
        DateTime startDate,
        int numberOfDays,
        CancellationToken cancellationToken = default)
    {
        const string query = """
            query GetDoctorAvailableDays($doctorId: String!, $startDate: DateTime!, $numberOfDays: Int!) {
                doctorAvailableDays(doctorId: $doctorId, startDate: $startDate, numberOfDays: $numberOfDays)
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
        {
            var result = await client.ExecuteAsync<List<DateTime>>(
                query,
                new
                {
                    doctorId,
                    startDate = startDate.ToUniversalTime().ToString("o"),
                    numberOfDays
                },
                "doctorAvailableDays");
            return result ?? new();
        });
    }

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
