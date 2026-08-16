namespace Basket.Basket.Features.DeleteBasket;

using global::Basket.Basket.Security;
using System.Security.Claims;

//public record DeleteBasketRequest(string UserName);
public record DeleteBasketResponse(Boolean IsSuccess);

public class DeleteBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapDelete("/basket/{userName}", async (String userName, ISender sender, ClaimsPrincipal user) =>
    {
      string? authenticatedUserName = BasketIdentity.GetUserName(user);
      if (string.IsNullOrWhiteSpace(authenticatedUserName) ||
          !string.Equals(userName, authenticatedUserName, StringComparison.OrdinalIgnoreCase))
      {
        return Results.Forbid();
      }

      DeleteBasketResult result = await sender.Send(new DeleteBasketCommand(authenticatedUserName));

      DeleteBasketResponse response = result.Adapt<DeleteBasketResponse>();

      return Results.Ok(response);
    })
    .Produces<DeleteBasketResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithSummary("Delete Basket")
    .WithDescription("Delete Basket")
    .RequireAuthorization();
  }
}
