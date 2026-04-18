using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Client.Services.AuthServices;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.RealTimeServices;
using PiedraAzul.Client.States;

namespace PiedraAzul.Client.Extensions;

public static class SharedClientServicesExtensions
{
    public static IServiceCollection AddSharedClientServices(this IServiceCollection services)
    {
        #region States
        services.AddScoped<UserState>();
        #endregion

        #region Auth Services
        services.AddScoped<AuthenticationService>();
        services.AddScoped<JwtService>();
        services.AddScoped<RefreshAuthClient>();
        services.AddScoped<ITokenService, TokenService>();
        #endregion

        #region GraphQL Feature Services
        services.AddScoped<GraphQLAvailabilityService>();
        services.AddScoped<GraphQLDoctorService>();
        services.AddScoped<GraphQLAppointmentService>();
        services.AddScoped<GraphQLPatientService>();
        #endregion

        #region Auth
        services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();
        services.AddAuthorizationCore();
        #endregion

        return services;
    }
}

public static class ClientWasmExtensions
{
    public static IServiceCollection AddClientWasm(this IServiceCollection services, string baseAddress, string hubUrl)
    {
        services.AddSharedClientServices();

        services.AddScoped<GraphQLHttpClient>(sp =>
        {
            var tokenService = sp.GetRequiredService<ITokenService>();
            var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
            return new GraphQLHttpClient(httpClient, tokenService);
        });

        #region SignalR
        services.AddScoped<IAppointmentHubService>(sp => new AppointmentHubService(hubUrl));
        #endregion

        return services;
    }
}

public static class ClientServerExtensions
{
    public static IServiceCollection AddClientServer(this IServiceCollection services, string graphqlUrl, string hubUrl)
    {
        services.AddSharedClientServices();

        services.AddScoped<GraphQLHttpClient>(sp =>
        {
            var tokenService = sp.GetRequiredService<ITokenService>();
            var httpClient = new HttpClient { BaseAddress = new Uri(graphqlUrl) };
            return new GraphQLHttpClient(httpClient, tokenService);
        });

        #region SignalR
        services.AddScoped<IAppointmentHubService>(sp => new AppointmentHubService(hubUrl));
        #endregion

        return services;
    }
}
