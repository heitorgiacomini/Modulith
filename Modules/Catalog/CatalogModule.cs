using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Behaviors;
using Shared.Data.Interceptors;

namespace Catalog;

public static class CatalogModule
{
	public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
	{
		// Add services to the container.
		// Api Endpoint services
		// Application Use Case services
		_ = services.AddMediatR(cfg =>
		{
			_ = cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
			_ = cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
		});
		_ = services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

		//Data — Infrastructure servlces
		String? connectionString = configuration.GetConnectionString("Database");

		_ = services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
		_ = services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

		_ = services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
		{
			_ = options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
			_ = options.UseNpgsql(connectionString);
		});


		_ = services.AddScoped<IDataSeeder, CatalogDataSeeder>();
		return services;
	}

	public static IApplicationBuilder UseCatalogModule(this IApplicationBuilder app)
	{
		// Api Endpoint services
		// Application Use Case services
		//Data — Infrastructure servlces

		_ = app.UseMigration<CatalogDbContext>();
		return app;
	}

}
