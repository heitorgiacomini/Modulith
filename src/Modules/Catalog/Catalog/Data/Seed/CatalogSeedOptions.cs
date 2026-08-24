namespace Catalog.Data.Seed;

public sealed class CatalogSeedOptions
{
	public const string SectionName = "Catalog";

	public Guid[] SeedTenantIds { get; set; } = [];
}
