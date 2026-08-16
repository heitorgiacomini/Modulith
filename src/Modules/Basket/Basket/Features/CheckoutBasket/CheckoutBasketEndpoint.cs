using Basket.Basket.Security;
using System.Security.Claims;

namespace Basket.Basket.Features.CheckoutBasket;

public record CheckoutBasketRequest(BasketCheckoutDto BasketCheckout);
public record CheckoutBasketResponse(bool IsSuccess);

public class CheckoutBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/basket/checkout", 
            async (CheckoutBasketRequest request, ISender sender, ClaimsPrincipal user) =>
            {
                string? userName = BasketIdentity.GetUserName(user);
                Guid? customerId = BasketIdentity.GetCustomerId(user);
                if (string.IsNullOrWhiteSpace(userName) || customerId is null)
                {
                    return Results.Problem(
                        "The access token must contain preferred_username and a UUID subject.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                BasketCheckoutDto checkout = request.BasketCheckout with
                {
                    UserName = userName,
                    CustomerId = customerId.Value
                };
                var command = new CheckoutBasketCommand(checkout);

                var result = await sender.Send(command);

                var response = result.Adapt<CheckoutBasketResponse>();

                return Results.Ok(response);
            })
        .WithName("CheckoutBasket")
        .Produces<CheckoutBasketResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Checkout Basket")
        .WithDescription("Checkout Basket")
        .RequireAuthorization();
    }
}
