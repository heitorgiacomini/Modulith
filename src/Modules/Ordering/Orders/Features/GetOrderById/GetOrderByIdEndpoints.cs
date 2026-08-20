using Ordering.Orders.Authorization;
using System.Security.Claims;

namespace Ordering.Orders.Features.GetOrderById;

//public record GetOrderByIdRequest();
public record GetOrderByIdResponse(OrderDto Order);

public class GetOrderByIdEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders/{id}", async (
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

            bool canReadAll = permission.HasScope(OrderingAuthorization.ReadAllScope);
            var result = await sender.Send(new GetOrderByIdQuery(id, permission.CustomerId, canReadAll));

            var response = result.Adapt<GetOrderByIdResponse>();

            return Results.Ok(response);
        })
        .WithName("GetOrderById")
        .Produces<GetOrderByIdResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Order By Id")
        .WithDescription("Get Order By Id")
        .RequireAuthorization(OrderingAuthorization.ReadPolicy);
    }
}
