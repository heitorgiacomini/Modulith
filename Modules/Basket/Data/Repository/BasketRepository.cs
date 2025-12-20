
namespace Basket.Data.Repository;

public class BasketRepository(BasketDbContext dbContext) : IBasketRepository
{
    public async Task<ShoppingCart> GetBasketAsync(String userName, Boolean asNoTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<ShoppingCart> query = dbContext.ShoppingCarts
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
        _ = dbContext.ShoppingCarts.Add(basket);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return basket;
    }

    public async Task<Boolean> DeleteBasketAsync(String userName, CancellationToken cancellationToken = default)
    {
        ShoppingCart basket = await this.GetBasketAsync(userName, false, cancellationToken);

        _ = dbContext.ShoppingCarts.Remove(basket);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken); ;
    }

}
