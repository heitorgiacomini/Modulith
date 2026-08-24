using Basket.Basket.Features.AddItemIntoBasket;
using Basket.Basket.Features.CheckoutBasket;
using Basket.Basket.Features.CreateBasket;
using Basket.Basket.Features.DeleteBasket;
using Basket.Basket.Features.GetBasket;
using Basket.Basket.Features.RemoveItemFromBasket;
using Riok.Mapperly.Abstractions;

namespace Basket.Basket.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class BasketMapper
{
    public static partial ShoppingCartDto ToDto(ShoppingCart cart);

    public static partial CreateBasketResponse ToResponse(CreateBasketResult result);

    public static partial AddItemIntoBasketResponse ToResponse(AddItemIntoBasketResult result);

    public static partial RemoveItemFromBasketResponse ToResponse(RemoveItemFromBasketResult result);

    public static partial CheckoutBasketResponse ToResponse(CheckoutBasketResult result);

    public static partial GetBasketResponse ToResponse(GetBasketResult result);

    public static partial DeleteBasketResponse ToResponse(DeleteBasketResult result);
}
