using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Ordering.Data;

namespace Ordering.Orders.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class OrderingQueries
{
  [UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]
  [UseFiltering]
  [UseSorting]
  public IQueryable<OrderListItem> Orders([Service] OrderingDbContext orderingDbContext)
  {
    return orderingDbContext.Orders
      .AsNoTracking()
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
      });
  }
}
