namespace Basket.Data.Repository;

public interface IBasketRepository
{
    Task<ShoppingCart> GetBasketAsync(String userName, Boolean asNoTracking = true, CancellationToken cancellationToken = default);
    Task<ShoppingCart> CreateBasketAsync(ShoppingCart basket, CancellationToken cancellationToken = default);
    Task<Boolean> DeleteBasketAsync(String userName, CancellationToken cancellationToken = default);
    Task<Int32> SaveChangesAsync(CancellationToken cancellationToken = default);
    //Task<Int32> SaveChangesAsync(String? userName = null, CancellationToken cancellationToken = default);
}
