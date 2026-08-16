namespace Basket.Basket.Features.AddItemIntoBasket;

using global::Basket.Basket.Security;
using System.Security.Claims;

public record AddItemIntoBasketRequest(String UserName, ShoppingCartItemDto ShoppingCartItem);
public record AddItemIntoBasketResponse(Guid Id);

public class AddItemIntoBasketEndpoint : ICarterModule
{
  public void AddRoutes(IEndpointRouteBuilder app)
  {
    _ = app.MapPost("/basket/{userName}/items",
        async ([FromRoute] String userName,
               [FromBody] AddItemIntoBasketRequest request,
               ISender sender,
               ClaimsPrincipal user) =>
        {
          string? authenticatedUserName = BasketIdentity.GetUserName(user);
          if (string.IsNullOrWhiteSpace(authenticatedUserName) ||
              !string.Equals(userName, authenticatedUserName, StringComparison.OrdinalIgnoreCase))
          {
            return Results.Forbid();
          }

          AddItemIntoBasketCommand command = new AddItemIntoBasketCommand(authenticatedUserName, request.ShoppingCartItem);

          var result = await sender.Send(command);

          var response = result.Adapt<AddItemIntoBasketResponse>();

          return Results.Created($"/basket/{response.Id}", response);
        })
    .Produces<AddItemIntoBasketResponse>(StatusCodes.Status201Created)
    .ProducesProblem(StatusCodes.Status400BadRequest)
    .WithSummary("Add Item Into Basket")
    .WithDescription("Add Item Into Basket")
    .RequireAuthorization();

  }
}
