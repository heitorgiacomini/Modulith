namespace Basket.Basket.Features.DeleteBasket;

//public record DeleteBasketRequest(string UserName);
public record DeleteBasketResponse(Boolean IsSuccess);

public class DeleteBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapDelete("/basket/{userName}", async (String userName, ISender sender) =>
    {
      DeleteBasketResult result = await sender.Send(new DeleteBasketCommand(userName));

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
