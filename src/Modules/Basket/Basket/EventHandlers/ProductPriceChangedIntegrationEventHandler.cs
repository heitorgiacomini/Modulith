using Basket.Basket.Features.UpdateItemPriceInBasket;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Messaging.Events;

namespace Basket.Basket.EventHandlers;
public class ProductPriceChangedIntegrationEventHandler
    (ISender sender, ICurrentTenant currentTenant, ILogger<ProductPriceChangedIntegrationEventHandler> logger)
    : IConsumer<ProductPriceChangedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductPriceChangedIntegrationEvent> context)
    {
        using IDisposable tenantScope = currentTenant.Change(context.Message.TenantId);
        logger.LogInformation("Integration Event handled: {IntegrationEvent}", context.Message.GetType().Name);

        // mediatr new command and handler to find products on basket and update price
        var command = new UpdateItemPriceInBasketCommand(context.Message.ProductId, context.Message.Price);
        var result = await sender.Send(command);

        if (!result.IsSuccess)
        {
            logger.LogError("Error updating price in basket for product id: {ProductId}", context.Message.ProductId);
        }

        logger.LogInformation("Price for product id: {ProductId} updated in basket", context.Message.ProductId);        
    }
}
