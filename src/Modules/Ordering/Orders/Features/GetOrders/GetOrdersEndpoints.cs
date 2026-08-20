using Ordering.Orders.Authorization;
using System.Security.Claims;

namespace Ordering.Orders.Features.GetOrders;

//public record GetOrdersRequest(PaginationRequest PaginationRequest);
public record GetOrdersResponse(PaginatedResult<OrderDto> Orders);

public class GetOrdersEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/orders", async (
            [AsParameters] PaginationRequest request,
            ISender sender,
            ClaimsPrincipal user,
            IOrderingPermissionEvaluator evaluator) =>
        {
            OrderingPermission? permission = evaluator.Evaluate(user);
            if (permission is null)
            {
                return Results.Unauthorized();
            }

            Guid? customerId = permission.HasScope(OrderingAuthorization.ReadAllScope)
                ? null
                : permission.CustomerId;
            var result = await sender.Send(new GetOrdersQuery(customerId, request));

            GetOrdersResponse response = result.Adapt<GetOrdersResponse>();

            return Results.Ok(response);
        })
        .WithName("GetOrders")
        .Produces<GetOrdersResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Orders")
        .WithDescription("Get Orders")
        .RequireAuthorization(OrderingAuthorization.ReadPolicy);
    }
}
