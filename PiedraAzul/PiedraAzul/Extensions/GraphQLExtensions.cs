using PiedraAzul.GraphQL;

namespace PiedraAzul.Extensions;

public static class GraphQLExtensions
{
    public static IServiceCollection AddPiedraAzulGraphQL(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddAuthorization();

        return services;
    }

    public static WebApplication MapGraphQLEndpoint(this WebApplication app)
    {
        app.MapGraphQL("/graphql");
        return app;
    }
}
