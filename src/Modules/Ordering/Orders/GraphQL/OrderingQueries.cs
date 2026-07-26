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
      .Select(order => new OrderListItem(
        order.Id,
        order.CustomerId,
        order.OrderName,
        order.Items.Count,
        order.Items.Sum(item => item.Price * item.Quantity)));
  }
}
