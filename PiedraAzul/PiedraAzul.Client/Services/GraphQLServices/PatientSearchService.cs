using PiedraAzul.Client.Models;
using PiedraAzul.Client.Models.GraphQL;
using PiedraAzul.Client.Services.Wrappers;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class PatientSearchService(GraphQLHttpClient graphQL)
{
    public async Task<Result<List<PatientSearchResultGQL>>> SearchAutoCompleteAsync(string query, int limit = 10)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string gqlQuery = """
                query SearchAutoComplete($query: String!) {
                    searchAutoCompletePatients(query: $query) {
                        id name identification phone type
                    }
                }
                """;

            var result = await graphQL.ExecuteAsync<List<PatientSearchResultGQL>>(
                gqlQuery,
                new { query },
                "searchAutoCompletePatients");

            return (result ?? []).Take(limit).ToList();
        });
    }

    /// <summary>
    /// Lookup público para el flujo de auto-agendamiento de invitados.
    /// Devuelve hash de verificación si es un guest existente, o tipo REGISTERED si tiene cuenta.
    /// Devuelve null si no existe.
    /// </summary>
    public async Task<Result<GuestLookupResultGQL?>> LookupByIdentificationAsync(string identification)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string gqlQuery = """
                query LookupGuest($identification: String!) {
                    lookupGuestByIdentification(identification: $identification) {
                        verificationHash hasPhone hasEmail maskedPhone maskedEmail type
                    }
                }
                """;

            return await graphQL.ExecuteAsync<GuestLookupResultGQL?>(
                gqlQuery,
                new { identification },
                "lookupGuestByIdentification");
        });
    }

    public async Task<Result<List<PatientSearchResultGQL>>> SearchPatientsAsync(string query, int limit = 10)
    {
        return await GraphQLExecutor.Execute(async () =>
        {
            const string gqlQuery = """
                query SearchPatients($query: String!, $limit: Int) {
                    searchPatients(query: $query, limit: $limit) {
                        id name identification phone type
                    }
                }
                """;

            var result = await graphQL.ExecuteAsync<List<PatientSearchResultGQL>>(
                gqlQuery,
                new { query, limit },
                "searchPatients");

            return result ?? [];
        });
    }
}
