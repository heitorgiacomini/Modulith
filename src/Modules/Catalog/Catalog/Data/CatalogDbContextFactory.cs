using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Data;
using Shared.Data.MultiTenancy;

namespace Catalog.Data;

public sealed class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
  public CatalogDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<CatalogDbContext>()
      .UseNpgsql(Environment.GetEnvironmentVariable("MIGRATION_CONNECTION")
        ?? "Host=localhost;Database=eshopdb;Username=eshopdb;Password=eshopdb").Options;
    return new CatalogDbContext(options, new DesignTimeDataFilter(), new CurrentTenant());
  }
}
