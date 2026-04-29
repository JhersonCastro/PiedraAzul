using Microsoft.Extensions.DependencyInjection;

namespace PiedraAzul.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediator(opts =>
            {
                opts.Assemblies = [typeof(DependencyInjection)];

                opts.ServiceLifetime = ServiceLifetime.Scoped;
            });

            return services;
        }
    }
}