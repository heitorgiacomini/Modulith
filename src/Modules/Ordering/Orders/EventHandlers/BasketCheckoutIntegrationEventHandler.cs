using MassTransit;
using Ordering.Orders.Features.CreateOrder;
using Shared.Messaging.Events;

namespace Ordering.Orders.EventHandlers;
public class BasketCheckoutIntegrationEventHandler
    (ISender sender, ICurrentTenant currentTenant, ILogger<BasketCheckoutIntegrationEventHandler> logger)
    : IConsumer<BasketCheckoutIntegrationEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutIntegrationEvent> context)
    {
        using IDisposable tenantScope = currentTenant.Change(context.Message.TenantId);
        logger.LogInformation("Integration Event handled: {IntegrationEvent}", context.Message.GetType().Name);

        // Create new order and start order fullfillment process
        var createOrderCommand = OrderingMapper.ToCommand(context.Message);
        await sender.Send(createOrderCommand);
    }
}
