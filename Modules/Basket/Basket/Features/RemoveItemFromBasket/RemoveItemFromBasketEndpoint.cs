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
               ISender sender) =>
        {
          RemoveItemFromBasketCommand command = new RemoveItemFromBasketCommand(userName, productId);

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
