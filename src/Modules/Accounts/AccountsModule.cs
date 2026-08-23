using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Data;
using Shared.Data.Interceptors;

namespace Accounts;

public static class AccountsModule
{
  public static IServiceCollection AddAccountsModule(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    string? connectionString = configuration.GetConnectionString("Database");

    services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesInterceptor, AuditableEntityInterceptor>());
    services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>());
    services.AddDbContext<AccountsDbContext>((serviceProvider, options) =>
    {
      options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
      options.UseNpgsql(connectionString);
    });

    return services;
  }

  public static IApplicationBuilder UseAccountsModule(this IApplicationBuilder app)
  {
    app.UseMigration<AccountsDbContext>();
    return app;
  }
}
