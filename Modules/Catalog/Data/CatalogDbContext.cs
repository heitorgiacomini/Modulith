namespace Catalog.Data;

public class CatalogDbContext : DbContext
{
	public CatalogDbContext(DbContextOptions<CatalogDbContext> options)
			: base(options)
	{
	}
	// Define DbSets for your entities here
	//public DbSet<Product> Products { get; set; }
	public DbSet<Product> Products => this.Set<Product>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		_ = modelBuilder.HasDefaultSchema("catalog");
		//modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
		_ = modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

		base.OnModelCreating(modelBuilder);
	}
}
