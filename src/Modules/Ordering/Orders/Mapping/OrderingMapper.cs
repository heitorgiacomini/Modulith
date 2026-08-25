using Ordering.Orders.Features.CreateOrder;
using Ordering.Orders.Features.DeleteOrder;
using Ordering.Orders.Features.GetOrderById;
using Ordering.Orders.Features.GetOrders;
using Riok.Mapperly.Abstractions;
using Shared.Messaging.Events;

namespace Ordering.Orders.Mapping;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
internal static partial class OrderingMapper
{
    public static partial OrderDto ToDto(Order order);

    public static partial List<OrderDto> ToDtos(List<Order> orders);

    [MapValue(nameof(OrderDto.Id), Use = nameof(EmptyGuid))]
    [MapValue(nameof(OrderDto.CustomerId), Use = nameof(EmptyGuid))]
    public static partial OrderDto ToDto(CreateOrderInput input);

    [MapValue(nameof(OrderItemDto.OrderId), Use = nameof(EmptyGuid))]
    private static partial OrderItemDto ToDto(CreateOrderItemInput input);

    public static partial CreateOrderCommand ToCommand(CreateOrderRequest request);

    [MapPropertyFromSource(nameof(CreateOrderFromCheckoutCommand.Order), Use = nameof(ToCheckoutOrderDto))]
    public static partial CreateOrderFromCheckoutCommand ToCommand(BasketCheckoutIntegrationEvent message);

    [MapValue(nameof(OrderDto.Id), Use = nameof(EmptyGuid))]
    [MapProperty(nameof(BasketCheckoutIntegrationEvent.UserName), nameof(OrderDto.OrderName))]
    [MapProperty(nameof(BasketCheckoutIntegrationEvent.Address), nameof(OrderDto.ShippingAddress))]
    [MapProperty(nameof(BasketCheckoutIntegrationEvent.Address), nameof(OrderDto.BillingAddress))]
    private static partial OrderDto ToCheckoutOrderDto(BasketCheckoutIntegrationEvent message);

    [MapValue(nameof(OrderItemDto.OrderId), Use = nameof(EmptyGuid))]
    private static partial OrderItemDto ToDto(BasketCheckoutItem item);

    public static Order ToDomain(OrderDto dto)
    {
        var order = Order.Create(
            Guid.NewGuid(),
            dto.CustomerId,
            $"{dto.OrderName}_{Random.Shared.Next()}",
            ToDomain(dto.ShippingAddress),
            ToDomain(dto.BillingAddress),
            ToDomain(dto.Payment));

        foreach (OrderItemDto item in dto.Items)
        {
            order.Add(item.ProductId, item.Quantity, item.Price);
        }

        return order;
    }

    public static partial CreateOrderResponse ToResponse(CreateOrderResult result);

    public static partial DeleteOrderResponse ToResponse(DeleteOrderResult result);

    public static partial GetOrderByIdResponse ToResponse(GetOrderByIdResult result);

    public static partial GetOrdersResponse ToResponse(GetOrdersResult result);

    private static Address ToDomain(AddressDto dto) => Address.Of(
        dto.FirstName,
        dto.LastName,
        dto.EmailAddress,
        dto.Phone,
        dto.AddressLine1,
        dto.AddressLine2,
        dto.City,
        dto.State,
        dto.PostalCode,
        dto.CountryCode);

    private static Payment ToDomain(PaymentDto dto) => Payment.Of(
        dto.Token,
        dto.CardholderName,
        dto.Brand,
        dto.Last4,
        dto.Expiration);

    private static Guid EmptyGuid() => Guid.Empty;
}
