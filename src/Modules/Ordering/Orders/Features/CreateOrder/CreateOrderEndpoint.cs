using Ordering.Orders.Authorization;
using System.Security.Claims;

namespace Ordering.Orders.Features.CreateOrder;

public record CreateOrderRequest(CreateOrderInput Order);
public record CreateOrderInput(
    string OrderName,
    AddressDto ShippingAddress,
    AddressDto BillingAddress,
    PaymentDto Payment,
    List<CreateOrderItemInput> Items);
public record CreateOrderItemInput(Guid ProductId, int Quantity, decimal Price);
public record CreateOrderResponse(Guid Id);

public class CreateOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (
            CreateOrderRequest request,
            ISender sender,
            ClaimsPrincipal user,
            IOrderingPermissionEvaluator evaluator) =>
        {
            OrderingPermission? permission = evaluator.Evaluate(user);
            if (permission is null)
            {
                return Results.Unauthorized();
            }

            var command = new CreateOrderCommand(new OrderDto(
                Id: Guid.Empty,
                CustomerId: permission.CustomerId,
                OrderName: request.Order.OrderName,
                ShippingAddress: request.Order.ShippingAddress,
                BillingAddress: request.Order.BillingAddress,
                Payment: request.Order.Payment,
                Items: request.Order.Items
                    .Select(item => new OrderItemDto(
                        Guid.Empty,
                        item.ProductId,
                        item.Quantity,
                        item.Price))
                    .ToList()));

            var result = await sender.Send(command);

            var response = result.Adapt<CreateOrderResponse>();

            return Results.Created($"/Orders/{response.Id}", response);
        })
        .WithName("CreateOrder")
        .Produces<CreateOrderResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Create Order")
        .WithDescription("Create Order")
        .RequireAuthorization(OrderingAuthorization.CreatePolicy);
    }
}
