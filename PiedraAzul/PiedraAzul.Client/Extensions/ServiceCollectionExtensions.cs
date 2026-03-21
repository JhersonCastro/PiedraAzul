using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.AspNetCore.Components.Authorization;
using PiedraAzul.Client.Services.AuthServices;
using PiedraAzul.Client.Services.GrpcServices;
using PiedraAzul.Client.Services.Interceptors;
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

            services.AddScoped<AuthInterceptor>();

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
            #endregion

            #region GRPC CHANNEL
            services.AddScoped(sp =>
            {
                var cookieHandler = sp.GetRequiredService<CookieHandler>();
                cookieHandler.InnerHandler = new HttpClientHandler();

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

            #region CALL INVOKER 
            services.AddScoped<CallInvoker>(sp =>
            {
                var channel = sp.GetRequiredService<GrpcChannel>();
                var interceptor = sp.GetRequiredService<AuthInterceptor>();

                return channel.Intercept(interceptor);
            });
            #endregion

            #region GRPC CLIENTS
            services.AddScoped(sp =>
                new AuthService.AuthServiceClient(
                    sp.GetRequiredService<CallInvoker>()));

            services.AddScoped(sp =>
                new AvailabilityService.AvailabilityServiceClient(
                    sp.GetRequiredService<CallInvoker>()));

            services.AddScoped(sp =>
                new DoctorService.DoctorServiceClient(
                    sp.GetRequiredService<CallInvoker>()));
            #endregion

            return services;
        }
    }

    public static class ClientServerExtensions
    {
        public static IServiceCollection AddClientServer(this IServiceCollection services, string grpcUrl)
        {
            services.AddSharedClientServices();

            #region GRPC CHANNEL
            services.AddScoped(sp =>
            {
                return GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions
                {
                    HttpHandler = new HttpClientHandler()
                });
            });
            #endregion

            #region CALL INVOKER 
            services.AddScoped<CallInvoker>(sp =>
            {
                var channel = sp.GetRequiredService<GrpcChannel>();
                var interceptor = sp.GetRequiredService<AuthInterceptor>();

                return channel.Intercept(interceptor);
            });
            #endregion

            #region GRPC CLIENTS
            services.AddScoped(sp =>
                new AuthService.AuthServiceClient(
                    sp.GetRequiredService<CallInvoker>()));

            services.AddScoped(sp =>
                new AvailabilityService.AvailabilityServiceClient(
                    sp.GetRequiredService<CallInvoker>()));

            services.AddScoped(sp =>
                new DoctorService.DoctorServiceClient(
                    sp.GetRequiredService<CallInvoker>()));
            #endregion

            return services;
        }
    }
}