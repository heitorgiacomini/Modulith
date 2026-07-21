using Keycloak.AuthServices.Authentication;
using System.Reflection;

namespace Api;

public partial class Program
{
  private const string FrontendCorsPolicy = "FrontendCors";

  private static async Task Main(String[] args)
  {
    WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(args);

    //webAppBuilder.Logging.ClearProviders();
    UseSerilog(webAppBuilder);

    Assembly catalogAssembly = typeof(CatalogModule).Assembly;
    Assembly basketAssembly = typeof(BasketModule).Assembly;
    Assembly orderingAssembly = typeof(OrderingModule).Assembly;
    // add services to the container.
    _ = webAppBuilder.Services
      .AddCarterWithAssemblies(
        catalogAssembly,
        basketAssembly,
        orderingAssembly
      );

    _ = webAppBuilder.Services.AddMediatRWithAssemblies(
      catalogAssembly,
      basketAssembly,
      orderingAssembly
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
      orderingAssembly
    );

    _ = webAppBuilder.Services.AddKeycloakWebApiAuthentication(webAppBuilder.Configuration);
    _ = webAppBuilder.Services.AddAuthorization();

    _ = webAppBuilder.Services.AddCors(options =>
    {
      options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
          .WithOrigins("http://localhost:4200")
          .AllowAnyHeader()
          .AllowAnyMethod());
    });

    //module services: catalog, basket, ordering
    _ = webAppBuilder.Services
      .AddCatalogModule(webAppBuilder.Configuration)
      .AddOrderingModule(webAppBuilder.Configuration)
      .AddBasketModule(webAppBuilder.Configuration);

    _ = webAppBuilder.Services.AddExceptionHandler<CustomExceptionHandler>();

    _ = webAppBuilder.Services
      .AddGraphQLServer()
      .AddQueryType<Api.GraphQL.CatalogQueries>();

    WebApplication webApp = webAppBuilder.Build();

    _ = webApp.MapCarter();
    _ = webApp.MapGraphQL();

    _ = webApp.UseSerilogRequestLogging();
    _ = webApp.UseExceptionHandler(options => { });
    _ = webApp.UseCors(FrontendCorsPolicy);

    _ = webApp.UseAuthentication();
    _ = webApp.UseAuthorization();

    _ = webApp
      .UseCatalogModule()
      .UseOrderingModule()
      .UseBasketModule();




    //Configure the HTTP request pipeline.

    //app.UseStaticFiles();

    //app.UseRouting();

    //app.UseAuthentication();

    //app.UseAuthorization();


    //app.UseEndpoints(endpoints =>
    //{
    //	endpoints.MapControllers();
    //});

    //webApp.Run();
    await webApp.RunAsync();
  }


  /// <summary>
  /// Configures Serilog as the logging provider for the application host.
  /// </summary>
  /// <param name="webAppBuilder">The <see cref="WebApplicationBuilder"/> used to configure the application and host.</param>
  /// <remarks>
  /// This method hooks Serilog into the generic host and instructs it to read
  /// its configuration (sinks, minimum levels, enrichers, etc.) from the
  /// application's configuration sources (for example, appsettings.json and environment variables).
  /// Call this before building the <see cref="WebApplication"/>.
  /// </remarks>
  public static void UseSerilog(WebApplicationBuilder webAppBuilder)
  {
    _ = webAppBuilder.Host
          .UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration)
        );
  }
}
