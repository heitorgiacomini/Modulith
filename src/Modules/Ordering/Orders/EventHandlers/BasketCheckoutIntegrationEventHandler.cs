using MassTransit;
using Ordering.Orders.Features.CreateOrder;
using Shared.Messaging.Events;

namespace Ordering.Orders.EventHandlers;
public class BasketCheckoutIntegrationEventHandler
    (ISender sender, ILogger<BasketCheckoutIntegrationEventHandler> logger)
    : IConsumer<BasketCheckoutIntegrationEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutIntegrationEvent> context)
    {
        logger.LogInformation("Integration Event handled: {IntegrationEvent}", context.Message.GetType().Name);

        // Create new order and start order fullfillment process
        var createOrderCommand = MapToCreateOrderCommand(context.Message);
        await sender.Send(createOrderCommand);
    }

    private CreateOrderCommand MapToCreateOrderCommand(BasketCheckoutIntegrationEvent message)
    {
        // Create full order with incoming event data
        var addressDto = new AddressDto(
            message.Address.FirstName, message.Address.LastName, message.Address.EmailAddress,
            message.Address.Phone, message.Address.AddressLine1, message.Address.AddressLine2,
            message.Address.City, message.Address.State, message.Address.PostalCode, message.Address.CountryCode);
        var paymentDto = new PaymentDto(
            message.Payment.Token, message.Payment.CardholderName, message.Payment.Brand,
            message.Payment.Last4, message.Payment.Expiration);
        var orderId = Guid.NewGuid();

        var orderDto = new OrderDto(
            Id: orderId,
            CustomerId: message.CustomerId,
            OrderName: message.UserName,
            ShippingAddress: addressDto,
            BillingAddress: addressDto,
            Payment: paymentDto,
            Items: message.Items
                .Select(item => new OrderItemDto(orderId, item.ProductId, item.Quantity, item.Price))
                .ToList());

        return new CreateOrderCommand(orderDto);
    }
}
