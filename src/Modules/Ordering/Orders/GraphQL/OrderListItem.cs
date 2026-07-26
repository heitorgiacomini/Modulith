namespace Ordering.Orders.GraphQL;

public sealed record OrderListItem(
  Guid Id,
  Guid CustomerId,
  string OrderName,
  int ItemCount,
  decimal TotalPrice);
