using PiedraAzul.GraphQL;
using PiedraAzul.GraphQL.Types;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Authorization;

namespace PiedraAzul.Extensions;

public static class GraphQLExtensions
{
    public static IServiceCollection AddPiedraAzulGraphQL(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddType<MFAStatusType>()
            .AddType<PatientTypeEnum>()
            .AddType<GuestLookupResultType>()
            .AddType<GuestDataType>()
            .AddAuthorization();

        return services;
    }

    public static WebApplication MapGraphQLEndpoint(this WebApplication app)
    {
        app.MapGraphQL("/graphql")
            .WithOptions(new GraphQLServerOptions
            {
                Tool = { Enable = app.Environment.IsDevelopment() }
            });

        return app;
    }
}
