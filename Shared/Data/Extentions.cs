using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data.Seed;

namespace Shared.Data;

public static class Extentions
{

	public static IApplicationBuilder UseMigration<TContext>(this IApplicationBuilder appBuilder)
		where TContext : DbContext
	{
		MigrateDatabaseAsync<TContext>(appBuilder.ApplicationServices).GetAwaiter().GetResult();
		SeedDataAsync(appBuilder.ApplicationServices).GetAwaiter().GetResult();
		return appBuilder;
	}

	private static async Task SeedDataAsync(IServiceProvider applicationServices)
	{
		using IServiceScope scope = applicationServices.CreateScope();
		IEnumerable<IDataSeeder> seeders = scope.ServiceProvider.GetServices<IDataSeeder>();
		foreach (IDataSeeder seeder in seeders)
		{
			await seeder.SeedAllAsync();
		}
	}

	private static async Task MigrateDatabaseAsync<TContext>(IServiceProvider applicationServices)
		where TContext : DbContext
	{
		using IServiceScope scope = applicationServices.CreateScope();
		TContext dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
		await dbContext.Database.MigrateAsync();
	}
}
