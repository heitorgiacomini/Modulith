using HotChocolate.Types;
using HotChocolate.Types.Composite;

namespace Ordering.Orders.GraphQL;

public sealed class OrderListItem
{
  public Guid Id { get; init; }

  public Guid CustomerId { get; init; }

  public string OrderName { get; init; } = string.Empty;

  public int ItemCount { get; init; }

  public decimal TotalPrice { get; init; }

  public List<OrderItemListItem> Items { get; init; } = [];
}

public sealed class OrderItemListItem
{
  public Guid ProductId { get; init; }

  public ProductReference Product => new(ProductId);

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
