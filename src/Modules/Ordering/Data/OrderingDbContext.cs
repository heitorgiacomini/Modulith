namespace Ordering.Data;
public class OrderingDbContext : DbContext, IDataFilterContext
{
    private readonly IDataFilter dataFilter;
    private readonly ICurrentTenant currentTenant;

    public OrderingDbContext(DbContextOptions<OrderingDbContext> options, IDataFilter dataFilter, ICurrentTenant currentTenant)
        : base(options)
    {
        this.dataFilter = dataFilter;
        this.currentTenant = currentTenant;
    }

    public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();
    public bool IsMultiTenantFilterEnabled => dataFilter.IsEnabled<IMultiTenant>();
    public Guid? CurrentTenantId => currentTenant.Id;

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("ordering");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        builder.ApplyDataFilters(this);
        base.OnModelCreating(builder);
    }
}
