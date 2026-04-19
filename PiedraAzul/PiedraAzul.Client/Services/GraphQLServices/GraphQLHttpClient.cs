using System.Net.Http.Json;
using System.Text.Json;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLClientException : Exception
{
    public GraphQLClientException(string message) : base(message) { }
}

public class GraphQLHttpClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<T?> ExecuteAsync<T>(string query, object? variables, string dataField)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query, variables })
        };

        var response = await httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new GraphQLClientException($"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {json}");

        var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Array &&
            errors.GetArrayLength() > 0)
        {
            var message = errors[0].TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "GraphQL error";
            throw new GraphQLClientException(message ?? "GraphQL error");
        }

        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty(dataField, out var field))
        {
            return JsonSerializer.Deserialize<T>(field.GetRawText(), JsonOptions);
        }

        return default;
    }
}
