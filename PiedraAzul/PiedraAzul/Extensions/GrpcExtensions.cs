using PiedraAzul.GrpcServices;

namespace PiedraAzul.Extensions
{
    public static class GrpcExtensions
    {
        public static WebApplication MapGrpcServices(this WebApplication app)
        {
            app.MapGrpcService<GrpcAuth>().EnableGrpcWeb();
            app.MapGrpcService<GrpcAvailability>().EnableGrpcWeb();
            app.MapGrpcService<GrpcAppointment>().EnableGrpcWeb();
            app.MapGrpcService<GrpcDoctor>().EnableGrpcWeb();
            app.MapGrpcService<GrpcPatient>().EnableGrpcWeb();

            return app;
        }
    }
}
