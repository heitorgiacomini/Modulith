using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Data;
using Ordering.Orders.Authorization;
using Ordering.Orders.GraphQL;
using Shared.Data;
using Shared.Data.Interceptors;

namespace Ordering;
public static class OrderingModule
{
    public const string GraphQLSchemaName = "ordering";

    public static IServiceCollection AddOrderingModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add services to the container.
        // 1. Api Endpoint services

        // 2. Application Use Case services

        // 3. Data - Infrastructure services
        var connectionString = configuration.GetConnectionString("Database");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<OrderingDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        services.AddSingleton<IOrderingPermissionEvaluator, OrderingPermissionEvaluator>();
        services.AddScoped<IAuthorizationHandler, OrderingScopeAuthorizationHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(OrderingAuthorization.CreatePolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new OrderingScopeRequirement(OrderingAuthorization.CreateOwnScope)));
            options.AddPolicy(OrderingAuthorization.ReadPolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new OrderingScopeRequirement(
                        OrderingAuthorization.ReadOwnScope,
                        OrderingAuthorization.ReadAllScope)));
            options.AddPolicy(OrderingAuthorization.DeletePolicy, policy =>
                policy.RequireAuthenticatedUser()
                    .AddRequirements(new OrderingScopeRequirement(
                        OrderingAuthorization.DeleteOwnScope,
                        OrderingAuthorization.DeleteAllScope)));
        });

        _ = services
            .AddGraphQLServer(GraphQLSchemaName)
            .AddOrderingGraphQL()
            .AddAuthorization()
            .AddFiltering()
            .AddSorting()
            .ModifyCostOptions(options => options.MaxFieldCost = 5_000);

        return services;
    }

    public static IApplicationBuilder UseOrderingModule(this IApplicationBuilder app)
    {
        // Configure the HTTP request pipeline.
        // 1. Use Api Endpoint services

        // 2. Use Application Use Case services

        // 3. Use Data - Infrastructure services
        app.UseMigration<OrderingDbContext>();

        return app;
    }
}

