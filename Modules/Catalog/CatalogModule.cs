
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data.Interceptors;

namespace Catalog
{
	public static class CatalogModule
	{
		public static IServiceCollection AddCatalogModule(this IServiceCollection services, IConfiguration configuration)
		{
			// Add services to the container.
			// Api Endpoint services
			// Application Use Case services
			//Data — Infrastructure servlces
			string? connectionString = configuration.GetConnectionString("Database");

			_ = services.AddDbContext<CatalogDbContext>(options =>
			{
				_ = options.AddInterceptors(new AuditableEntityInterceptor());
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
}
