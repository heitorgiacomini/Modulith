using HotChocolate;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;

namespace Ordering.Orders.GraphQL;

[Key("id")]
public sealed class OrderListItem
{
  public Guid Id { get; init; }

  public Guid CustomerId { get; init; }

  public string OrderName { get; init; } = string.Empty;

  public int ItemCount { get; init; }

  public decimal TotalPrice { get; init; }

  public List<OrderItemListItem> Items { get; init; } = [];

  [ReferenceResolver]
  public static async Task<OrderListItem?> ResolveReferenceAsync(
    Guid id,
    [Service] OrderingDbContext dbContext,
    CancellationToken cancellationToken)
  {
    return await dbContext.Orders
      .AsNoTracking()
      .Where(order => order.Id == id)
      .Select(order => new OrderListItem
      {
        Id = order.Id,
        CustomerId = order.CustomerId,
        OrderName = order.OrderName,
        ItemCount = order.Items.Count,
        TotalPrice = order.Items.Sum(item => item.Price * item.Quantity),
        Items = order.Items
          .Select(item => new OrderItemListItem
          {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
          })
          .ToList()
      })
      .FirstOrDefaultAsync(cancellationToken);
  }
}

public sealed class OrderItemListItem
{
  public Guid ProductId { get; init; }

  public int Quantity { get; init; }

  public decimal Price { get; init; }
}
