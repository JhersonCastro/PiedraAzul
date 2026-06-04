using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Client.Services.AdminServices;
using PiedraAzul.Client.Services.AuthServices;
using PiedraAzul.Client.Services.GraphQLServices;
using PiedraAzul.Client.Services.RealTimeServices;
using PiedraAzul.Client.Services.Schedule;
using PiedraAzul.Client.States;
using PiedraAzul.Client.Services;

namespace PiedraAzul.Client.Extensions;

public static class SharedClientServicesExtensions
{
    public static IServiceCollection AddSharedClientServices(this IServiceCollection services)
    {
        #region States
        services.AddScoped<UserState>();
        services.AddScoped<UIModeState>();
        services.AddScoped<DoctorPortalState>();
        services.AddScoped<ClientIpState>();
        // El servidor sobrescribe IClientIpResolver con una implementación que lee HttpContext.
        services.AddScoped<IClientIpResolver, NullClientIpResolver>();
        #endregion

        #region UI Mode
        services.AddScoped<UIModeService>();
        #endregion

        #region Auth Services
        services.AddScoped<AuthenticationService>();
        services.AddScoped<PasskeyService>();
        #endregion

        #region GraphQL Feature Services
        services.AddScoped<GraphQLAvailabilityService>();
        services.AddScoped<GraphQLDoctorService>();
        services.AddScoped<GraphQLAppointmentService>();
        services.AddScoped<GraphQLPatientService>();
        services.AddScoped<ScheduleConfigAdminService>();
        services.AddScoped<UserManagementService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<PatientSearchService>();
        services.AddScoped<ConsultationService>();
        services.AddScoped<IScheduleConfigService, ScheduleConfigService>();
        #endregion

        #region Offline / PWA
        services.AddScoped<OfflineCacheService>();
        #endregion

        #region Tour
        services.AddScoped<DriverTourService>();
        #endregion

        #region Auth
        services.AddScoped<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
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
            new GraphQLHttpClient(new HttpClient { BaseAddress = new Uri(baseAddress) }));

        services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

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

        // GraphQL client registered here is overridden in server Program.cs with cookie forwarding
        services.AddScoped<GraphQLHttpClient>(sp =>
            new GraphQLHttpClient(new HttpClient { BaseAddress = new Uri(graphqlUrl) }));

        services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(graphqlUrl) });

        #region SignalR
        services.AddScoped<IAppointmentHubService>(sp => new AppointmentHubService(hubUrl));
        #endregion

        return services;
    }
}
