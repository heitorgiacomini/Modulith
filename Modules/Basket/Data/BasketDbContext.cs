namespace Basket.Data;

public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options)
        : base(options) { }

    public DbSet<ShoppingCart> ShoppingCarts => this.Set<ShoppingCart>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => this.Set<ShoppingCartItem>();
    //public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        _ = builder.HasDefaultSchema("basket");
        _ = builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);
    }
}
