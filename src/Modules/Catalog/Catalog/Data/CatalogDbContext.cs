namespace Catalog.Data;

public class CatalogDbContext : DbContext, IDataFilterContext
{
	private readonly IDataFilter dataFilter;

	public CatalogDbContext(DbContextOptions<CatalogDbContext> options, IDataFilter dataFilter)
			: base(options)
	{
		this.dataFilter = dataFilter;
	}

	public bool IsSoftDeleteFilterEnabled => this.dataFilter.IsEnabled<ISoftDelete>();
	// Define DbSets for your entities here
	//public DbSet<Product> Products { get; set; }
	public DbSet<Product> Products => this.Set<Product>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.HasDefaultSchema("catalog");
		//modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
		_ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		_ = modelBuilder.ApplySoftDeleteQueryFilters(this);

		base.OnModelCreating(modelBuilder);
	}
}
