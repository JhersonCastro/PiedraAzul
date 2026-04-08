//using PiedraAzul.RealTime.Hubs;
using PiedraAzul.RealTime.Hubs;

namespace PiedraAzul.Extensions;
public static class HubExtensions
{
    public static WebApplication MapHubs(this WebApplication app)
    {
        app.MapHub<AppointmentHub>("/hubs/appointments");
        return app;
    }
}