using PiedraAzul.GraphQL;
using PiedraAzul.GraphQL.Types;
using PiedraAzul.Contracts.DTOs;
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
            .AddType<GuestDataDto>()
            .AddAuthorization()
            .AddDiagnosticEventListener<GraphQLDiagnosticEventListener>();

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
