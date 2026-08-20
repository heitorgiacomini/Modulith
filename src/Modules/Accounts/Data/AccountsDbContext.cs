namespace Accounts.Data;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options) : DbContext(options)
{
  public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
  public DbSet<SavedAddress> SavedAddresses => Set<SavedAddress>();
  public DbSet<SavedPaymentMethod> SavedPaymentMethods => Set<SavedPaymentMethod>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    builder.HasDefaultSchema("accounts");
    builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    base.OnModelCreating(builder);
  }
}
