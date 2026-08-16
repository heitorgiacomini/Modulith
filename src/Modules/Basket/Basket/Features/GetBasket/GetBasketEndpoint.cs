namespace Basket.Basket.Features.GetBasket;

using global::Basket.Basket.Security;
using System.Security.Claims;

//public record GetBasketRequest(string UserName); 
public record GetBasketResponse(ShoppingCartDto ShoppingCart);

public class GetBasketEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/basket/{userName}", async (string userName, ISender sender, ClaimsPrincipal user) =>
        {
            string? authenticatedUserName = BasketIdentity.GetUserName(user);
            if (string.IsNullOrWhiteSpace(authenticatedUserName) ||
                !string.Equals(userName, authenticatedUserName, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid();
            }

            var result = await sender.Send(new GetBasketQuery(authenticatedUserName));

            var response = result.Adapt<GetBasketResponse>();

            return Results.Ok(response);
        })
        .Produces<GetBasketResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Basket")
        .WithDescription("Get Basket")
        .RequireAuthorization();
  }
}
