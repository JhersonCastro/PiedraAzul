using PiedraAzul.Client.Models.GraphQL;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLPatientService(GraphQLHttpClient client)
{
    public async Task<List<PatientSearchResultGQL>> SearchAutoCompletePatientsAsync(string query, int limit)
    {
        const string gqlQuery = """
            query SearchAutoComplete($query: String!) {
                searchAutoCompletePatients(query: $query) {
                    id name identification phone type
                }
            }
            """;

        var result = await client.ExecuteAsync<List<PatientSearchResultGQL>>(
            gqlQuery,
            new { query },
            "searchAutoCompletePatients");

        return (result ?? new()).Take(limit).ToList();
    }

    public async Task<List<PatientSearchResultGQL>> SearchPatientsAsync(string query, int limit)
    {
        const string gqlQuery = """
            query SearchPatients($query: String!, $limit: Int) {
                searchPatients(query: $query, limit: $limit) {
                    id name identification phone type
                }
            }
            """;

        var result = await client.ExecuteAsync<List<PatientSearchResultGQL>>(
            gqlQuery,
            new { query, limit },
            "searchPatients");

        return result ?? new();
    }
}
