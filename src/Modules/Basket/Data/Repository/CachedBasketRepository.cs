using Basket.Data.JsonConverters;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Basket.Data.Repository;

public class CachedBasketRepository(
  IBasketRepository repository,
  IDistributedCache distributedCache,
  ICurrentTenant currentTenant) : IBasketRepository
{
  private readonly JsonSerializerOptions _options = new JsonSerializerOptions
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new ShoppingCartConverter(), new ShoppingCartItemConverter() }
  };

  public async Task<ShoppingCart> GetBasketAsync(String userName, Boolean asNoTracking = true, CancellationToken cancellationToken = default)
  {
    string cacheKey = GetCacheKey(userName);
    //if (!asNoTracking)
    //{
    //  return await repository.GetBasketAsync(userName, false, cancellationToken);
    //}

    //var cachedBasket = await distributedCache.GetStringAsync(userName, cancellationToken);
    //if (!String.IsNullOrEmpty(cachedBasket))
    //{
    //  return JsonSerializer.Deserialize<ShoppingCart>(cachedBasket)!;
    //}
    //var basket = await repository.GetBasketAsync(userName, asNoTracking, cancellationToken);
    //await distributedCache.SetStringAsync(userName, JsonSerializer.Serialize(basket), cancellationToken);
    //return basket;

    if (asNoTracking)
    {
      String? cachedBasket = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
      if (!string.IsNullOrWhiteSpace(cachedBasket) &&
        JsonSerializer.Deserialize<ShoppingCart>(cachedBasket, this._options) is { } deserializedBasket)
      {
        return deserializedBasket;
      }
    }

    ShoppingCart basket = await repository.GetBasketAsync(userName, asNoTracking, cancellationToken);

    await distributedCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(basket, _options), cancellationToken);
    return basket;

  }
  public async Task<ShoppingCart> CreateBasketAsync(ShoppingCart basket, CancellationToken cancellationToken = default)
  {
    _ = await repository.CreateBasketAsync(basket, cancellationToken);
    await distributedCache.SetStringAsync(GetCacheKey(basket.UserName), JsonSerializer.Serialize(basket, _options), cancellationToken);
    return basket;
  }

  public async Task<Boolean> DeleteBasketAsync(String userName, CancellationToken cancellationToken = default)
  {
    Boolean result = await repository.DeleteBasketAsync(userName, cancellationToken);
    await distributedCache.RemoveAsync(GetCacheKey(userName), cancellationToken);
    return result;
  }


  public async Task<Int32> SaveChangesAsync(String? userName = null, CancellationToken cancellationToken = default)
  {
    Int32 result = await repository.SaveChangesAsync(userName, cancellationToken);

    if (userName.HasContent())
    {
      await distributedCache.RemoveAsync(GetCacheKey(userName!), cancellationToken);
    }

    return result;
  }

  private string GetCacheKey(string userName) =>
    $"{currentTenant.Id ?? throw new InvalidOperationException("An active tenant is required.")}:basket:{userName}";
}
