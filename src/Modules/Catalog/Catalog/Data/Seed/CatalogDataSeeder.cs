using Microsoft.Extensions.Options;

namespace Catalog.Data.Seed;

public class CatalogDataSeeder(
	CatalogDbContext dbContext,
	ICurrentTenant currentTenant,
	IOptions<CatalogSeedOptions> options) : IDataSeeder
{
	public async Task SeedAllAsync()
	{
		foreach (Guid tenantId in options.Value.SeedTenantIds.Distinct())
		{
			using IDisposable tenantScope = currentTenant.Change(tenantId);
			if (!await dbContext.Products.AnyAsync())
			{
				await dbContext.Products.AddRangeAsync(InitialData.CreateProducts(tenantId));
				_ = await dbContext.SaveChangesAsync();
			}
		}
	}
}
