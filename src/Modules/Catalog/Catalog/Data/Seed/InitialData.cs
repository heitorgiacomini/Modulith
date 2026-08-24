namespace Catalog.Data.Seed;

public static class InitialData
{
	public static IEnumerable<Product> CreateProducts(Guid tenantId)
		=>
		[
			Product.Create(CreateProductId(tenantId, "iphone-x"), "IPhone X", ["category1"], "Long description", "imagefile", 500),
			Product.Create(CreateProductId(tenantId, "samsung-10"), "Samsung 10", ["category1"], "Long description", "imagefile", 400),
			Product.Create(CreateProductId(tenantId, "huawei-plus"), "Huawei Plus", ["category2"], "Long description", "imagefile", 650),
			Product.Create(CreateProductId(tenantId, "xiaomi-mi"), "Xiaomi Mi", ["category2"], "Long description", "imagefile", 450)
		];

	private static Guid CreateProductId(Guid tenantId, string productKey)
		=> DeterministicGuid.CreateVersion5(tenantId, $"catalog:{productKey}");
}
