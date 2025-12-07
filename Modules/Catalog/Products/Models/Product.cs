

namespace Catalog.Products.Models
{
	public class Product : Aggregate<Guid>
	{
		public string Name { get; private set; } = default!;
		public List<string> Category { get; set; } = [];
		public string Description { get; set; } = default!;
		public string ImageFile { get; set; } = default!;
		public decimal Price { get; private set; }


		public static Product Create(Guid id, string name, List<string> category, string description, string imageFile, decimal price)
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

		public void Update(string name, List<string> category, string description, string imageFile, decimal price)
		{
			ArgumentException.ThrowIfNullOrEmpty(name);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

			Name = name;
			Category = category;
			Description = description;
			ImageFile = imageFile;
			Price = price;

			if (Price != price)
			{
				Price = price;
				AddDomainEvent(new ProductPriceChangedEvent(this));
			}
		}
	}
}
