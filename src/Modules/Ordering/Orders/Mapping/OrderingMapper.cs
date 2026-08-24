using Ordering.Orders.Features.CreateOrder;
using Ordering.Orders.Features.DeleteOrder;
using Ordering.Orders.Features.GetOrderById;
using Ordering.Orders.Features.GetOrders;
using Riok.Mapperly.Abstractions;

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

    public static partial CreateOrderResponse ToResponse(CreateOrderResult result);

    public static partial DeleteOrderResponse ToResponse(DeleteOrderResult result);

    public static partial GetOrderByIdResponse ToResponse(GetOrderByIdResult result);

    public static partial GetOrdersResponse ToResponse(GetOrdersResult result);

    private static Guid EmptyGuid() => Guid.Empty;
}
