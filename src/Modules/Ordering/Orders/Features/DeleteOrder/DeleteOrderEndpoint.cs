using Ordering.Orders.Authorization;
using System.Security.Claims;

namespace Ordering.Orders.Features.DeleteOrder;

//public record DeleteOrderRequest(Guid Id);
public record DeleteOrderResponse(bool IsSuccess);

public class DeleteOrderEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/orders/{id}", async (
            Guid id,
            ISender sender,
            ClaimsPrincipal user,
            IOrderingPermissionEvaluator evaluator) =>
        {
            OrderingPermission? permission = evaluator.Evaluate(user);
            if (permission is null)
            {
                return Results.Unauthorized();
            }

            bool canDeleteAll = permission.HasScope(OrderingAuthorization.DeleteAllScope);
            var result = await sender.Send(new DeleteOrderCommand(
                id,
                permission.CustomerId,
                canDeleteAll));

            var response = result.Adapt<DeleteOrderResponse>();

            return Results.Ok(response);
        })
        .WithName("DeleteOrder")
        .Produces<DeleteOrderResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .WithSummary("Delete Order")
        .WithDescription("Delete Order")
        .RequireAuthorization(OrderingAuthorization.DeletePolicy);
    }
}
