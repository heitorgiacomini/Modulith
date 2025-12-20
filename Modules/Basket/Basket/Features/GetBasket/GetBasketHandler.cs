namespace Basket.Basket.Features.GetBasket;

public record GetBasketQuery(String UserName)
    : IQuery<GetBasketResult>;
public record GetBasketResult(ShoppingCartDto ShoppingCart);

internal class GetBasketHandler(IBasketRepository repository)
    : IQueryHandler<GetBasketQuery, GetBasketResult>
{
    public async Task<GetBasketResult> Handle(GetBasketQuery query, CancellationToken cancellationToken)
    {
        // get basket with userName

        ShoppingCart basket = await repository.GetBasketAsync(query.UserName, true, cancellationToken);
        //mapping basket entity to shoppingcartdto
        ShoppingCartDto basketDto = basket.Adapt<ShoppingCartDto>();

        return new GetBasketResult(basketDto);
    }
}
