using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Data;
using Shared.Data.MultiTenancy;

namespace Ordering.Data;

public sealed class OrderingDbContextFactory : IDesignTimeDbContextFactory<OrderingDbContext>
{
  public OrderingDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<OrderingDbContext>()
      .UseNpgsql(Environment.GetEnvironmentVariable("MIGRATION_CONNECTION")
        ?? "Host=localhost;Database=eshopdb;Username=eshopdb;Password=eshopdb").Options;
    return new OrderingDbContext(options, new DesignTimeDataFilter(), new CurrentTenant());
  }
}
