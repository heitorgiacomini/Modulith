using MassTransit;
using Shared.Messaging.Events;

namespace Catalog.Products.Events.EventHandlers;

public class ProductPriceChangedEventHandler
  (IBus bus, ILogger<ProductPriceChangedEventHandler> logger)
    : INotificationHandler<ProductPriceChangedEvent>
{

  public async Task Handle(ProductPriceChangedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Domain Event handled: {DomainEvent}", notification.GetType().Name);

    //return Task.CompletedTask;

    // Publish product price changed integration event for update basket prices
    ProductPriceChangedIntegrationEvent integrationEvent = new ProductPriceChangedIntegrationEvent
    {
      ProductId = notification.Product.Id,
      Name = notification.Product.Name,
      Category = notification.Product.Category,
      Description = notification.Product.Description,
      ImageFile = notification.Product.ImageFile,
      Price = notification.Product.Price //set updated product price
    };

    await bus.Publish(integrationEvent, cancellationToken);
  }
}
