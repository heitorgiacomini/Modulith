namespace Ordering.Data;
public class OrderingDbContext : DbContext, IDataFilterContext
{
    private readonly IDataFilter dataFilter;

    public OrderingDbContext(DbContextOptions<OrderingDbContext> options, IDataFilter dataFilter)
        : base(options) => this.dataFilter = dataFilter;

    public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("ordering");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.ApplySoftDeleteQueryFilters(this);
        base.OnModelCreating(builder);
    }
}
