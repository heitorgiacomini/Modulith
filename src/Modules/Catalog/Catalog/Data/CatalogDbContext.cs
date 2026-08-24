namespace Catalog.Data;

public class CatalogDbContext : DbContext, IDataFilterContext
{
	private readonly IDataFilter dataFilter;
	private readonly ICurrentTenant currentTenant;

	public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IDataFilter dataFilter, ICurrentTenant currentTenant)
			: base(options)
	{
		this.dataFilter = dataFilter;
		this.currentTenant = currentTenant;
	}

	public bool IsSoftDeleteFilterEnabled => this.dataFilter.IsEnabled<ISoftDelete>();
	public bool IsMultiTenantFilterEnabled => this.dataFilter.IsEnabled<IMultiTenant>();
	public Guid? CurrentTenantId => this.currentTenant.Id;
	// Define DbSets for your entities here
	//public DbSet<Product> Products { get; set; }
	public DbSet<Product> Products => this.Set<Product>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.HasDefaultSchema("catalog");
		//modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
		_ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		_ = modelBuilder.ApplyDataFilters(this);

		base.OnModelCreating(modelBuilder);
	}
}
