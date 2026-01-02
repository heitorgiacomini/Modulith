using System.Text.Json.Serialization;

namespace Basket.Basket.Models;

public class ShoppingCartItem : Entity<Guid>
{
  public Guid ShoppingCartId { get; private set; } = default!;
  public Guid ProductId { get; private set; } = default!;
  public Int32 Quantity { get; internal set; } = default!;
  public String Color { get; private set; } = default!;

  // will comes from Catalog module
  public Decimal Price { get; private set; } = default!;
  public String ProductName { get; private set; } = default!;

  internal ShoppingCartItem(Guid shoppingCartId, Guid productId, Int32 quantity, String color, Decimal price, String productName)
  {
    this.ShoppingCartId = shoppingCartId;
    this.ProductId = productId;
    this.Quantity = quantity;
    this.Color = color;
    this.Price = price;
    this.ProductName = productName;
  }

  [JsonConstructor]
  public ShoppingCartItem(Guid id, Guid shoppingCartId, Guid productId, Int32 quantity, String color, Decimal price, String productName)
  {
    this.Id = id;
    this.ShoppingCartId = shoppingCartId;
    this.ProductId = productId;
    this.Quantity = quantity;
    this.Color = color;
    this.Price = price;
    this.ProductName = productName;
  }

  public void UpdatePrice(Decimal newPrice)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newPrice);
    this.Price = newPrice;
  }
}
