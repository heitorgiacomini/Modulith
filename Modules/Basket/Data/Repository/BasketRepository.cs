namespace Basket.Data.Repository;

public class BasketRepository(BasketDbContext basketDbContext) : IBasketRepository
{
  public async Task<ShoppingCart> GetBasketAsync(String userName, Boolean asNoTracking = true, CancellationToken cancellationToken = default)
  {
    IQueryable<ShoppingCart> query = basketDbContext.ShoppingCarts
      .Include(x => x.Items)
      .Where(x => x.UserName == userName);

    if (asNoTracking)
    {
      _ = query.AsNoTracking();
    }

    ShoppingCart? basket = await query.SingleOrDefaultAsync(cancellationToken);
    return basket ?? throw new BasketNotFoundException(userName);
  }

  public async Task<ShoppingCart> CreateBasketAsync(ShoppingCart basket, CancellationToken cancellationToken = default)
  {
    _ = basketDbContext.ShoppingCarts.Add(basket);
    _ = await basketDbContext.SaveChangesAsync(cancellationToken);
    return basket;
  }

  public async Task<Boolean> DeleteBasketAsync(String userName, CancellationToken cancellationToken = default)
  {
    ShoppingCart basket = await this.GetBasketAsync(userName, false, cancellationToken);

    _ = basketDbContext.ShoppingCarts.Remove(basket);
    _ = await basketDbContext.SaveChangesAsync(cancellationToken);

    return true;
  }

  public async Task<Int32> SaveChangesAsync(String? userName = null, CancellationToken cancellationToken = default)
  {
    return await basketDbContext.SaveChangesAsync(cancellationToken);
  }
}
