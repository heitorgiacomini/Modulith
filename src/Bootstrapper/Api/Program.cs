using Accounts.Data;
using Basket.Data;
using Catalog.Data;
using Ordering.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Shared.Data.Seed;
using Shared.Messaging.Outbox;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Reflection;
using Api.Infrastructure;
using Shared.Data.Auditing;
using Shared.Data.Filtering;
using Shared.Data.MultiTenancy;
using Shared.Hosting.Auditing;
using Shared.Hosting.Observability;
using Shared.Hosting.RateLimiting;

namespace Api;

public partial class Program
{
  private const string FrontendCorsPolicy = "FrontendCors";

  private static async Task Main(String[] args)
  {
    WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(
      args.Where(argument => argument != "--migrate").ToArray());

    _ = webAppBuilder.AddEshopObservability("eshop-api");
    _ = webAppBuilder.Services.AddEshopAuditTrail(webAppBuilder.Configuration);

    Assembly catalogAssembly = typeof(CatalogModule).Assembly;
    Assembly basketAssembly = typeof(BasketModule).Assembly;
    Assembly orderingAssembly = typeof(OrderingModule).Assembly;
    Assembly accountsAssembly = typeof(AccountsModule).Assembly;
    // add services to the container.
    _ = webAppBuilder.Services
      .AddCarterWithAssemblies(
        catalogAssembly,
        basketAssembly,
        orderingAssembly,
        accountsAssembly
      );

    _ = webAppBuilder.Services.AddMediatRWithAssemblies(
      catalogAssembly,
      basketAssembly,
      orderingAssembly,
      accountsAssembly
    );

    //<summary>
    //AddStackExchangeRedisCache
    //</summary >
    _ = webAppBuilder.Services.AddStackExchangeRedisCache(options =>
    {
      options.Configuration = webAppBuilder.Configuration.GetConnectionString("Redis");
    });

    _ = webAppBuilder.Services.AddMassTransitWithAssemblies(
      webAppBuilder.Configuration,
      catalogAssembly,
      basketAssembly,
      orderingAssembly,
      accountsAssembly
    );

    _ = webAppBuilder.Services.AddKeycloakWebApiAuthentication(webAppBuilder.Configuration);
    string publicIssuer = webAppBuilder.Configuration["Keycloak:PublicIssuer"]
      ?? webAppBuilder.Configuration["Keycloak:public-issuer"]
      ?? throw new InvalidOperationException("Keycloak:public-issuer is required.");
    webAppBuilder.Services.PostConfigure<JwtBearerOptions>(
      JwtBearerDefaults.AuthenticationScheme,
      options =>
      {
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuer = publicIssuer;
      });
    _ = webAppBuilder.Services.AddAuthorization(options =>
    {
      options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new CurrentTenantRequirement())
        .AddRequirements(new OrganizationRoleRequirement("customer", "admin"))
        .Build();
    });
    _ = webAppBuilder.Services.AddSingleton<IAuthorizationHandler, CurrentTenantAuthorizationHandler>();
    _ = webAppBuilder.Services.AddSingleton<IAuthorizationHandler, OrganizationRoleAuthorizationHandler>();
    _ = webAppBuilder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, TenantAuthorizationMiddlewareResultHandler>();
    _ = webAppBuilder.Services.AddApplicationRateLimiting(
      webAppBuilder.Configuration,
      FrontendCorsPolicy);
    _ = webAppBuilder.Services.AddDataFilters();
    _ = webAppBuilder.Services.AddHttpContextAccessor();
    _ = webAppBuilder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

    _ = webAppBuilder.Services.AddCors(options =>
    {
      options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
          .WithOrigins(webAppBuilder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200", "http://127.0.0.1:4200"])
          .AllowAnyHeader()
          .AllowAnyMethod());
    });

    //module services: catalog, basket, ordering
    _ = webAppBuilder.Services
      .AddCatalogModule(webAppBuilder.Configuration)
      .AddOrderingModule(webAppBuilder.Configuration)
      .AddBasketModule(webAppBuilder.Configuration)
      .AddAccountsModule(webAppBuilder.Configuration);

    webAppBuilder.Services.AddHealthChecks()
      .AddCheck<DatabaseReadinessCheck<CatalogDbContext>>("catalog-db", tags: ["ready"])
      .AddCheck<DatabaseReadinessCheck<BasketDbContext>>("basket-db", tags: ["ready"])
      .AddCheck<DatabaseReadinessCheck<OrderingDbContext>>("ordering-db", tags: ["ready"])
      .AddCheck<DatabaseReadinessCheck<AccountsDbContext>>("accounts-db", tags: ["ready"]);
    webAppBuilder.Services.AddHostedService<IntegrationOutboxDispatcher<CatalogDbContext>>();
    webAppBuilder.Services.AddHostedService<IntegrationOutboxDispatcher<BasketDbContext>>();
    webAppBuilder.Services.AddHostedService<AuditExportWorker<CatalogDbContext>>();
    webAppBuilder.Services.AddHostedService<AuditExportWorker<BasketDbContext>>();
    webAppBuilder.Services.AddHostedService<AuditExportWorker<OrderingDbContext>>();
    webAppBuilder.Services.AddHostedService<AuditExportWorker<AccountsDbContext>>();

    _ = webAppBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();

    WebApplication webApp = webAppBuilder.Build();

    if (args.Contains("--migrate", StringComparer.Ordinal))
    {
      await using var scope = webApp.Services.CreateAsyncScope();
      DbContext[] contexts = [
        scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
        scope.ServiceProvider.GetRequiredService<BasketDbContext>(),
        scope.ServiceProvider.GetRequiredService<OrderingDbContext>(),
        scope.ServiceProvider.GetRequiredService<AccountsDbContext>()];
      foreach (DbContext context in contexts)
        await context.Database.MigrateAsync();
      if (webAppBuilder.Configuration.GetValue("Database:Seed", false))
        foreach (IDataSeeder seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
          await seeder.SeedAllAsync();
      await webApp.DisposeAsync();
      return;
    }

    webApp.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    webApp.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => true }).AllowAnonymous();

    _ = webApp.MapCarter();
    _ = webApp.MapGraphQL("/graphql/catalog", CatalogModule.GraphQLSchemaName);
    _ = webApp.MapGraphQL("/graphql/basket", BasketModule.GraphQLSchemaName);
    _ = webApp.MapGraphQL("/graphql/ordering", OrderingModule.GraphQLSchemaName);
    _ = webApp.MapGraphQLSchema("/graphql/catalog/schema.graphqls", CatalogModule.GraphQLSchemaName)
      .AllowAnonymous();
    _ = webApp.MapGraphQLSchema("/graphql/basket/schema.graphqls", BasketModule.GraphQLSchemaName)
      .AllowAnonymous();
    _ = webApp.MapGraphQLSchema("/graphql/ordering/schema.graphqls", OrderingModule.GraphQLSchemaName)
      .AllowAnonymous();

    _ = webApp.UseEshopRequestLogging();
    _ = webApp.UseExceptionHandler(options => { });
    _ = webApp.UseAuthentication();
    _ = webApp.UseRateLimiter();
    _ = webApp.UseCors(FrontendCorsPolicy);
    _ = webApp.UseMiddleware<CurrentTenantMiddleware>();
    _ = webApp.UseAuthorization();

    await webApp.RunAsync();
  }
}
