namespace Accounts.Data;

public sealed class AccountsDbContext(
  DbContextOptions<AccountsDbContext> options,
  IDataFilter dataFilter,
  ICurrentTenant currentTenant) : DbContext(options), IDataFilterContext
{
  public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();
  public bool IsMultiTenantFilterEnabled => dataFilter.IsEnabled<IMultiTenant>();
  public Guid? CurrentTenantId => currentTenant.Id;

  public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
  public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();
  public DbSet<SavedPaymentMethod> SavedPaymentMethods => Set<SavedPaymentMethod>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.HasDefaultSchema("accounts");
    builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    builder.ApplyDataFilters(this);
    base.OnModelCreating(builder);
  }
}
