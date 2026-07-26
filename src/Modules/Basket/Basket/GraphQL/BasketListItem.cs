namespace Basket.Basket.GraphQL;

public sealed record BasketListItem(
  Guid Id,
  string UserName,
  int ItemCount,
  decimal TotalPrice);
