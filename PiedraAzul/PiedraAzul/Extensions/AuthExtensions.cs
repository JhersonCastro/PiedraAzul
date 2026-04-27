using Microsoft.AspNetCore.Identity;

namespace PiedraAzul.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/denied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);

                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });

        services.AddAuthorization();
        return services;
    }
}
