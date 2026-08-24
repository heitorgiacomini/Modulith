using Catalog;
using Catalog.Data.Seed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace SharedTests;

public sealed class CatalogSeedTests
{
	private static readonly Guid AcmeId = new("11111111-1111-1111-1111-111111111111");
	private static readonly Guid ContosoId = new("22222222-2222-2222-2222-222222222222");

	[Fact]
	public void ProductIdsAreStableVersion5Guids()
	{
		Guid[] first = InitialData.CreateProducts(AcmeId).Select(product => product.Id).ToArray();
		Guid[] second = InitialData.CreateProducts(AcmeId).Select(product => product.Id).ToArray();

		Assert.Equal(first, second);
		Assert.All(first, productId => Assert.Equal('5', productId.ToString("D")[14]));
	}

	[Fact]
	public void DifferentTenantsReceiveNonEmptyDisjointProductIds()
	{
		Guid[] acme = InitialData.CreateProducts(AcmeId).Select(product => product.Id).ToArray();
		Guid[] contoso = InitialData.CreateProducts(ContosoId).Select(product => product.Id).ToArray();

		Assert.NotEmpty(acme);
		Assert.NotEmpty(contoso);
		Assert.Empty(acme.Intersect(contoso));
	}

	[Fact]
	public void ModuleBindsConfiguredSeedTenantIds()
	{
		IConfiguration configuration = Configuration(
			new Dictionary<string, string?>
			{
				["Catalog:SeedTenantIds:0"] = AcmeId.ToString(),
				["Catalog:SeedTenantIds:1"] = ContosoId.ToString()
			});
		ServiceCollection services = new();
		_ = services.AddCatalogModule(configuration);

		using ServiceProvider provider = services.BuildServiceProvider();
		Assert.Equal([AcmeId, ContosoId], provider.GetRequiredService<IOptions<CatalogSeedOptions>>().Value.SeedTenantIds);
	}

	[Fact]
	public void EmptyProductionConfigurationDoesNotSeedTenants()
	{
		ServiceCollection services = new();
		_ = services.AddCatalogModule(Configuration([]));

		using ServiceProvider provider = services.BuildServiceProvider();
		Assert.Empty(provider.GetRequiredService<IOptions<CatalogSeedOptions>>().Value.SeedTenantIds);
	}

	[Fact]
	public void EmptyGuidConfigurationIsRejected()
	{
		ServiceCollection services = new();
		_ = services.AddCatalogModule(Configuration(
			new Dictionary<string, string?> { ["Catalog:SeedTenantIds:0"] = Guid.Empty.ToString() }));

		using ServiceProvider provider = services.BuildServiceProvider();
		Assert.Throws<OptionsValidationException>(
			() => provider.GetRequiredService<IOptions<CatalogSeedOptions>>().Value);
	}

	private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values)
		=> new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
