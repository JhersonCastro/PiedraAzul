using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PiedraAzul.Client.Services.AuthServices;

namespace PiedraAzul.Client.Services.GraphQLServices;

public class GraphQLClientException : Exception
{
    public GraphQLClientException(string message) : base(message)
    {
    }
}
public class GraphQLHttpClient
{
    private readonly HttpClient httpClient;
    private readonly ITokenService tokenService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GraphQLHttpClient(HttpClient httpClient, ITokenService tokenService)
    {
        this.httpClient = httpClient;
        this.tokenService = tokenService;
    }

    public async Task<T?> ExecuteAsync<T>(
        string query,
        object? variables,
        string dataField)
    {
        var token = await tokenService.GetAccessTokenAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new
            {
                query,
                variables
            })
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await httpClient.SendAsync(request);

        // 🔥 IMPORTANTE: leer SIEMPRE el body (aunque sea 400)
        var json = await response.Content.ReadAsStringAsync();

        // ❌ Si HTTP falla, lanza error con el body real
        if (!response.IsSuccessStatusCode)
        {
            throw new GraphQLClientException(
                $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase}): {json}"
            );
        }

        var doc = JsonDocument.Parse(json);

        // ❌ Errores GraphQL (aunque HTTP sea 200)
        if (doc.RootElement.TryGetProperty("errors", out var errors) &&
            errors.ValueKind == JsonValueKind.Array &&
            errors.GetArrayLength() > 0)
        {
            var message =
                errors[0].TryGetProperty("message", out var msg)
                    ? msg.GetString()
                    : "GraphQL error";

            throw new GraphQLClientException(message ?? "GraphQL error");
        }

        // ✅ Extraer data
        if (doc.RootElement.TryGetProperty("data", out var data) &&
            data.TryGetProperty(dataField, out var field))
        {
            return JsonSerializer.Deserialize<T>(
                field.GetRawText(),
                JsonOptions
            );
        }

        return default;
    }
}