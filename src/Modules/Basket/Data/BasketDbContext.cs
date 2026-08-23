namespace Basket.Data;

public class BasketDbContext : DbContext, IDataFilterContext
{
  private readonly IDataFilter dataFilter;

  public BasketDbContext(DbContextOptions<BasketDbContext> options, IDataFilter dataFilter)
      : base(options) => this.dataFilter = dataFilter;

  public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();

  public DbSet<ShoppingCart> ShoppingCarts => this.Set<ShoppingCart>();
  public DbSet<ShoppingCartItem> ShoppingCartItems => this.Set<ShoppingCartItem>();
  public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

  protected override void OnModelCreating(ModelBuilder builder)
  {
    _ = builder.HasDefaultSchema("basket");
    _ = builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    _ = builder.ApplySoftDeleteQueryFilters(this);
    base.OnModelCreating(builder);
  }
}
