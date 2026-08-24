using Ordering.Orders.Authorization;
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
            ISender sender) =>
        {
            var command = new CreateOrderCommand(OrderingMapper.ToDto(request.Order));

            var result = await sender.Send(command);

            var response = OrderingMapper.ToResponse(result);

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
