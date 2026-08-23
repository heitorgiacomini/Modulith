namespace Accounts.Data;

public sealed class AccountsDbContext(
  DbContextOptions<AccountsDbContext> options,
  IDataFilter dataFilter) : DbContext(options), IDataFilterContext
{
  public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();

  public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
  public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();
  public DbSet<SavedPaymentMethod> SavedPaymentMethods => Set<SavedPaymentMethod>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.HasDefaultSchema("accounts");
    builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    builder.ApplySoftDeleteQueryFilters(this);
    base.OnModelCreating(builder);
  }
}
