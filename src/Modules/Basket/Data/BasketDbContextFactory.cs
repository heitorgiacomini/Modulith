using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Data;
using Shared.Data.MultiTenancy;

namespace Basket.Data;

public sealed class BasketDbContextFactory : IDesignTimeDbContextFactory<BasketDbContext>
{
  public BasketDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<BasketDbContext>()
      .UseNpgsql(Environment.GetEnvironmentVariable("MIGRATION_CONNECTION")
        ?? "Host=localhost;Database=eshopdb;Username=eshopdb;Password=eshopdb").Options;
    return new BasketDbContext(options, new DesignTimeDataFilter(), new CurrentTenant());
  }
}
