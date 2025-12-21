using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shared.Data.Interceptors;

namespace Basket;

public static class BasketModule
{
  public static IServiceCollection AddBasketModule(this IServiceCollection services, IConfiguration configuration)
  {

    _ = services.AddScoped<IBasketRepository, BasketRepository>();
    _ = services.Decorate<IBasketRepository, CachedBasketRepository>();
    //_ = services.AddScoped<IBasketRepository>(provider =>
    //{
    //  BasketRepository basketRepository = provider.GetRequiredService<BasketRepository>();
    //  return new CachedBasketRepository(basketRepository, provider.GetRequiredService<IDistributedCache>());
    //});

    _ = services.AddScoped<IBasketRepository, CachedBasketRepository>();

    String? connectionString = configuration.GetConnectionString("Database");

    _ = services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
    _ = services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

    _ = services.AddDbContext<BasketDbContext>((serviceProvider, options) =>
    {
      _ = options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
      _ = options.UseNpgsql(connectionString);
    });


    //_ = services.AddScoped<IDataSeeder, BasketDataSeeder>();
    return services;
  }

  public static IApplicationBuilder UseBasketModule(this IApplicationBuilder app)
  {

    _ = app.UseMigration<BasketDbContext>();
    return app;
  }

}
