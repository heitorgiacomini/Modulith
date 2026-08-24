namespace Basket.Basket.Domain;

public class ShoppingCartItem : FullAuditedEntity<Guid>, IMultiTenant
{
  public Guid? TenantId { get; set; }
  public Guid ShoppingCartId { get; private set; } = default!;
  public Guid ProductId { get; private set; } = default!;
  public Int32 Quantity { get; internal set; } = default!;
  public String Color { get; private set; } = default!;

  // will comes from Catalog module
  public Decimal Price { get; private set; } = default!;
  public String ProductName { get; private set; } = default!;

  private ShoppingCartItem()
  {
  }

  internal static ShoppingCartItem Create(
    Guid shoppingCartId,
    Guid productId,
    Int32 quantity,
    String color,
    Decimal price,
    String productName)
  {
    return new ShoppingCartItem
    {
      ShoppingCartId = shoppingCartId,
      ProductId = productId,
      Quantity = quantity,
      Color = color,
      Price = price,
      ProductName = productName
    };
  }

  internal static ShoppingCartItem Restore(
    Guid id,
    Guid shoppingCartId,
    Guid productId,
    Int32 quantity,
    String color,
    Decimal price,
    String productName)
  {
    ShoppingCartItem item = Create(shoppingCartId, productId, quantity, color, price, productName);
    item.Id = id;
    return item;
  }

  public void UpdatePrice(Decimal newPrice)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newPrice);
    this.Price = newPrice;
  }
}
