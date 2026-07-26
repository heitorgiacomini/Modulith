using Basket.Data;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Basket.Basket.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class BasketQueries
{
  [UseOffsetPaging(IncludeTotalCount = true, DefaultPageSize = 20, MaxPageSize = 20)]
  [UseFiltering]
  [UseSorting]
  public IQueryable<BasketListItem> Baskets([Service] BasketDbContext basketDbContext)
  {
    return basketDbContext.ShoppingCarts
      .AsNoTracking()
      .Select(cart => new BasketListItem(
        cart.Id,
        cart.UserName,
        cart.Items.Count,
        cart.Items.Sum(item => item.Price * item.Quantity)));
  }
}
