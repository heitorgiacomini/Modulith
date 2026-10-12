using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Shared.Data;
using Shared.Data.MultiTenancy;

namespace Accounts.Data;

public sealed class AccountsDbContextFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
  public AccountsDbContext CreateDbContext(string[] args)
  {
    var options = new DbContextOptionsBuilder<AccountsDbContext>()
      .UseNpgsql(Environment.GetEnvironmentVariable("MIGRATION_CONNECTION")
        ?? "Host=localhost;Database=eshopdb;Username=eshopdb;Password=eshopdb").Options;
    return new AccountsDbContext(options, new DesignTimeDataFilter(), new CurrentTenant());
  }
}
