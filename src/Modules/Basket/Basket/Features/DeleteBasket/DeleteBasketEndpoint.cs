namespace Basket.Basket.Features.DeleteBasket;

//public record DeleteBasketRequest(string UserName);
public record DeleteBasketResponse(Boolean IsSuccess);

public class DeleteBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapDelete("/basket/{userName}", async (String userName, ISender sender, ICurrentUser currentUser) =>
    {
      string? authenticatedUserName = currentUser.UserName;
      if (string.IsNullOrWhiteSpace(authenticatedUserName) ||
          !string.Equals(userName, authenticatedUserName, StringComparison.OrdinalIgnoreCase))
      {
        return Results.Forbid();
      }

      DeleteBasketResult result = await sender.Send(new DeleteBasketCommand(authenticatedUserName));

      DeleteBasketResponse response = BasketMapper.ToResponse(result);

      return Results.Ok(response);
    })
    .Produces<DeleteBasketResponse>(StatusCodes.Status200OK)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithSummary("Delete Basket")
    .WithDescription("Delete Basket")
    .RequireAuthorization();
  }
}
