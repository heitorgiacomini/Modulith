using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace Basket.Basket.GraphQL;

[Key("id")]
public sealed class BasketListItem
{
  public Guid Id { get; init; }

  public string UserName { get; init; } = string.Empty;

  public int ItemCount { get; init; }

  public decimal TotalPrice { get; init; }

  public List<BasketItemListItem> Items { get; init; } = [];

  [ReferenceResolver]
  public static async Task<BasketListItem?> ResolveReferenceAsync(
    Guid id,
    [Service] BasketDbContext dbContext,
    CancellationToken cancellationToken)
  {
    return await dbContext.ShoppingCarts
      .AsNoTracking()
      .Where(cart => cart.Id == id)
      .Select(cart => new BasketListItem
      {
        Id = cart.Id,
        UserName = cart.UserName,
        ItemCount = cart.Items.Count,
        TotalPrice = cart.Items.Sum(item => item.Price * item.Quantity),
        Items = cart.Items
          .Select(item => new BasketItemListItem
          {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Color = item.Color,
            Quantity = item.Quantity,
            Price = item.Price
          })
          .ToList()
      })
      .FirstOrDefaultAsync(cancellationToken);
  }
}

public sealed class BasketItemListItem
{
  public Guid ProductId { get; init; }

  public string ProductName { get; init; } = string.Empty;

  public string Color { get; init; } = string.Empty;

  public int Quantity { get; init; }

  public decimal Price { get; init; }
}