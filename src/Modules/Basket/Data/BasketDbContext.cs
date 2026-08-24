namespace Basket.Data;

public class BasketDbContext : DbContext, IDataFilterContext
{
  private readonly IDataFilter dataFilter;
  private readonly ICurrentTenant currentTenant;

  public BasketDbContext(
    DbContextOptions<BasketDbContext> options,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant) : base(options)
  {
    this.dataFilter = dataFilter;
    this.currentTenant = currentTenant;
  }

  public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();
  public bool IsMultiTenantFilterEnabled => dataFilter.IsEnabled<IMultiTenant>();
  public Guid? CurrentTenantId => currentTenant.Id;

  public DbSet<ShoppingCart> ShoppingCarts => this.Set<ShoppingCart>();
  public DbSet<ShoppingCartItem> ShoppingCartItems => this.Set<ShoppingCartItem>();
  public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    _ = builder.HasDefaultSchema("basket");
    _ = builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    _ = builder.ApplyDataFilters(this);
    base.OnModelCreating(builder);
  }
}
