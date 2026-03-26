using Grpc.Net.Client;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using PiedraAzul.ApplicationServices.AutoCompleteServices;
using PiedraAzul.ApplicationServices.Mapping;
using PiedraAzul.Client.Services;
using PiedraAzul.Client.States;
using Shared.Grpc;
using System.Security.Claims;
using System.Text;

namespace PiedraAzul.Extensions
{
    public static  class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLucene(this IServiceCollection services, string luceneIndexPath, IndexWriter? writer)
        {
            var luceneVersion = LuceneVersion.LUCENE_48;
            var indexPath = luceneIndexPath;

            var dir = FSDirectory.Open(indexPath);
            var analyzer = new StandardAnalyzer(luceneVersion);
            var indexConfig = new IndexWriterConfig(luceneVersion, analyzer);
            writer = new IndexWriter(dir, indexConfig);

            // Services
            services.AddSingleton<Analyzer>(analyzer);
            services.AddSingleton<IndexWriter>(writer);

            services.AddSingleton<IPatientAutocompleteService, PatientAutocompleteService>(sp =>
            {
                return new PatientAutocompleteService(luceneIndexPath);
            });


            return services;
        }

        public static IServiceCollection AddAuthenticationAndAuthorization(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),

                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.Name
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var authHeader = ctx.Request.Headers["Authorization"].FirstOrDefault();

                        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                        {
                            ctx.Token = authHeader.Substring("Bearer ".Length).Trim();
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();
            return services;
        }

        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            services.AddSingleton<PatientMapper>();


            return services;
        }
    }
}
