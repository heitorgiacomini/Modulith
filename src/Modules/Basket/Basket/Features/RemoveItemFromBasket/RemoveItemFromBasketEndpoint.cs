namespace Basket.Basket.Features.RemoveItemFromBasket;

//public record RemoveItemFromBasketRequest(string UserName, Guid ProductId);
public record RemoveItemFromBasketResponse(Guid Id);

public class RemoveItemFromBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapDelete("/basket/{userName}/items/{productId}",
        async ([FromRoute] String userName,
               [FromRoute] Guid productId,
               ISender sender,
               ICurrentUser currentUser) =>
        {
          string? authenticatedUserName = currentUser.UserName;
          if (string.IsNullOrWhiteSpace(authenticatedUserName) ||
              !string.Equals(userName, authenticatedUserName, StringComparison.OrdinalIgnoreCase))
          {
            return Results.Forbid();
          }

          RemoveItemFromBasketCommand command = new RemoveItemFromBasketCommand(authenticatedUserName, productId);

          var result = await sender.Send(command);

          var response = result.Adapt<RemoveItemFromBasketResponse>();

          return Results.Ok(response);
        })
    .Produces<RemoveItemFromBasketResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithSummary("Remove Item From Basket")
    .WithDescription("Remove Item From Basket")
    .RequireAuthorization();
  }
}
