using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Ordering.Data;
using Ordering.Orders.Authorization;
using System.Security.Claims;

namespace Ordering.Orders.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class OrderingQueries
{
  [Authorize(Policy = OrderingAuthorization.ReadPolicy)]
  [UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]
  [UseFiltering]
  [UseSorting]
  public IQueryable<OrderListItem> Orders(
    [Service] OrderingDbContext orderingDbContext,
    ClaimsPrincipal user,
    [Service] IOrderingPermissionEvaluator evaluator)
  {
    OrderingPermission permission = evaluator.Evaluate(user)
      ?? throw new GraphQLException("Authentication is required.");

    IQueryable<Order> orders = orderingDbContext.Orders.AsNoTracking();
    if (!permission.HasScope(OrderingAuthorization.ReadAllScope))
    {
      orders = orders.Where(order => order.CustomerId == permission.CustomerId);
    }

    return orders
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
