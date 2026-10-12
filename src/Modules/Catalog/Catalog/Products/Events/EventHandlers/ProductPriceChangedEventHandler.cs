using Shared.Messaging.Outbox;
using MassTransit;
using Shared.Messaging.Events;

namespace Catalog.Products.Events.EventHandlers;

public class ProductPriceChangedEventHandler
  (CatalogDbContext dbContext, ICurrentTenant currentTenant, ILogger<ProductPriceChangedEventHandler> logger)
    : INotificationHandler<ProductPriceChangedEvent>
{

  public Task Handle(ProductPriceChangedEvent notification, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "Domain Event handled: {DomainEvent}", notification.GetType().Name);

    //return Task.CompletedTask;

    // Publish product price changed integration event for update basket prices
    ProductPriceChangedIntegrationEvent integrationEvent = new ProductPriceChangedIntegrationEvent
    {
      TenantId = currentTenant.Id ?? throw new InvalidOperationException("An active tenant is required."),
      ProductId = notification.Product.Id,
      Name = notification.Product.Name,
      Category = notification.Product.Category,
      Description = notification.Product.Description,
      ImageFile = notification.Product.ImageFile,
      Price = notification.Product.Price //set updated product price
    };

    dbContext.Add(IntegrationOutboxMessage.From(integrationEvent));
    return Task.CompletedTask;
  }
}
