using HotChocolate.Types;
using HotChocolate.Types.Composite;

namespace Basket.Basket.GraphQL;

public sealed class BasketListItem
{
  public Guid Id { get; init; }

  public string UserName { get; init; } = string.Empty;

  public int ItemCount { get; init; }

  public decimal TotalPrice { get; init; }

  public List<BasketItemListItem> Items { get; init; } = [];
}

public sealed class BasketItemListItem
{
  public Guid ProductId { get; init; }

  public ProductReference Product => new(ProductId);

  public string ProductName { get; init; } = string.Empty;

  public string Color { get; init; } = string.Empty;

  public int Quantity { get; init; }

  public decimal Price { get; init; }
}

public sealed record ProductReference(Guid Id);

public sealed class ProductReferenceType : ObjectType<ProductReference>
{
  protected override void Configure(IObjectTypeDescriptor<ProductReference> descriptor)
  {
    descriptor.Name("ProductListItem");
    ((IObjectTypeDescriptor)descriptor).EntityKey("id");
  }
}
