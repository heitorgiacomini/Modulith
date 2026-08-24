

namespace Catalog.Products.Domain;

public class Product : FullAuditedAggregate<Guid>, IMultiTenant
{
	public Guid? TenantId { get; set; }
	public String Name { get; private set; } = default!;
	public List<String> Category { get; set; } = [];
	public String Description { get; set; } = default!;
	public String ImageFile { get; set; } = default!;
	public Decimal Price { get; private set; }

  private Product()
  {

  }
  public static Product Create(Guid id, String name, List<String> category, String description, String imageFile, Decimal price)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

		Product product = new()
		{
			Id = id,
			Name = name,
			Category = category,
			Description = description,
			ImageFile = imageFile,
			Price = price
		};
		product.AddDomainEvent(new ProductCreatedEvent(product));
		return product;
	}

	public void Update(String name, List<String> category, String description, String imageFile, Decimal price)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

		this.Name = name;
		this.Category = category;
		this.Description = description;
		this.ImageFile = imageFile;

		if (this.Price != price)
		{
			this.Price = price;
			this.AddDomainEvent(new ProductPriceChangedEvent(this));
		}
	}
}
