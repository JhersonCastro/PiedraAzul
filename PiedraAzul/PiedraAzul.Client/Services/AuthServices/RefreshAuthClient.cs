using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Text.Json;

namespace PiedraAzul.Client.Services.AuthServices;

public class RefreshAuthClient
{
    private readonly HttpClient httpClient;

    public RefreshAuthClient(NavigationManager navigation)
    {
        httpClient = new HttpClient { BaseAddress = new Uri(navigation.BaseUri) };
    }

    public async Task<string?> RefreshTokenAsync()
    {
        const string mutation = """
            mutation RefreshToken {
                refreshToken { accessToken }
            }
            """;

        try
        {
            var response = await httpClient.PostAsJsonAsync("/graphql", new { query = mutation });

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("refreshToken", out var rt) &&
                rt.TryGetProperty("accessToken", out var token))
            {
                return token.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
