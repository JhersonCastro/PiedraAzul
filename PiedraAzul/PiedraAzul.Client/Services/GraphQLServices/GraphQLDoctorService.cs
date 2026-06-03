using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.Wrappers;
using PiedraAzul.Contracts.Enums;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLDoctorService(GraphQLHttpClient client)
{
    public async Task<List<DoctorGQL>> GetDoctorsByTypeAsync(string doctorType, bool onlyAvailable = false)
    {
        const string query = """
            query GetDoctorsByType($doctorType: DoctorSpecialty!, $onlyAvailable: Boolean!) {
                doctorsByType(doctorType: $doctorType, onlyAvailable: $onlyAvailable) {
                    doctorId userId name specialty avatarUrl licenseNumber notes isAvailable
                }
            }
            """;

        var result = await client.ExecuteAsync<List<DoctorGQL>>(
            query,
            new { doctorType, onlyAvailable },
            "doctorsByType");

        return result ?? new();
    }

    public async Task<Result<bool>> ToggleMyAvailabilityAsync()
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string mutation = """
                mutation ToggleMyAvailability {
                    toggleMyAvailability {
                        doctorId isAvailable
                    }
                }
                """;

            var result = await client.ExecuteAsync<DoctorAvailabilityGQL>(mutation, null, "toggleMyAvailability");
            return result?.IsAvailable ?? false;
        });
    }

    public async Task<Result<DoctorGQL?>> GetDoctorAsync(string doctorId)
    {
        const string query = """
            query GetDoctor($doctorId: String!) {
                doctor(doctorId: $doctorId) {
                    doctorId userId name specialty avatarUrl licenseNumber notes
                }
            }
            """;

        return await GraphQLExecutor.Execute(async () =>
            await client.ExecuteAsync<DoctorGQL>(query, new { doctorId }, "doctor"));
    }
}
