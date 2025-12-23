using System.Security.Claims;

namespace Basket.Basket.Features.CreateBasket;

public record CreateBasketRequest(ShoppingCartDto ShoppingCart);
public record CreateBasketResponse(Guid Id);

public class CreateBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapPost("/basket", async (CreateBasketRequest request, ISender sender, ClaimsPrincipal user) =>
    {
      String? userName = user.Identity?.Name;
      var updatedShoppingCart = request.ShoppingCart with { UserName = userName };

      CreateBasketCommand command = new CreateBasketCommand(updatedShoppingCart);
      //CreateBasketCommand command = request.Adapt<CreateBasketCommand>();

      CreateBasketResult result = await sender.Send(command);

      CreateBasketResponse response = result.Adapt<CreateBasketResponse>();

      return Results.Created($"/basket/{response.Id}", response);
    })
    .Produces<CreateBasketResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithSummary("Create Basket")
    .WithDescription("Create Basket")
    .RequireAuthorization();
  }
}
