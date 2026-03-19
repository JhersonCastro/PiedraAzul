using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Client.DelegatingHandlers;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.States;
using Shared.Grpc;

namespace PiedraAzul.Client.Extensions
{
    public static class SharedClientServicesExtensions
    {
        public static IServiceCollection AddSharedClientServices(this IServiceCollection services)
        {
            #region States
            services.AddScoped<UserState>();
            #endregion

            #region Services
            services.AddScoped<AuthenticationService>();
            services.AddScoped<JwtService>();
            services.AddScoped<RefreshAuthClient>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<GrpcAvailability>();
            services.AddScoped<GrpcDoctorService>();
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
        public static IServiceCollection AddClientWasm(this IServiceCollection services, string baseAddress)
        {
            services.AddSharedClientServices();

            #region Handlers
            services.AddScoped<CookieHandler>();
            services.AddScoped<HttpDelegatingHandler>();
            #endregion

            #region GRPC CHANNEL
            services.AddScoped(sp =>
            {
                var cookieHandler = sp.GetRequiredService<CookieHandler>();
                var authHandler = sp.GetRequiredService<HttpDelegatingHandler>();

                cookieHandler.InnerHandler = authHandler;
                authHandler.InnerHandler = new HttpClientHandler();

                var grpcHandler = new GrpcWebHandler(
                    GrpcWebMode.GrpcWeb,
                    cookieHandler);

                return GrpcChannel.ForAddress(
                    baseAddress,
                    new GrpcChannelOptions
                    {
                        HttpHandler = grpcHandler
                    });
            });
            #endregion

            #region GRPC CLIENT
            services.AddScoped(sp =>
                new AuthService.AuthServiceClient(
                    sp.GetRequiredService<GrpcChannel>()));

            services.AddScoped(sp =>
                new AvailabilityService.AvailabilityServiceClient(
                    sp.GetRequiredService<GrpcChannel>()));
            services.AddScoped(sp =>
                new DoctorService.DoctorServiceClient(
                    sp.GetRequiredService<GrpcChannel>()));
            #endregion

            return services;
        }
    }

    public static class ClientServerExtensions
    {
        public static IServiceCollection AddClientServer(this IServiceCollection services, string grpcUrl)
        {
            services.AddSharedClientServices();

            services.AddScoped(sp =>
            {
                var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = new HttpClientHandler()
                });

                return new AuthService.AuthServiceClient(channel);
            });
            services.AddScoped(sp =>
            {
                var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = new HttpClientHandler()
                });

                return new AvailabilityService.AvailabilityServiceClient(channel);
            });
            services.AddScoped(sp =>
            {
                var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = new HttpClientHandler()
                });

                return new DoctorService.DoctorServiceClient(channel);
            });
            return services;
        }
    }
}