using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.Events;

namespace Shared.Messaging.Outbox;

public sealed class IntegrationOutboxMessage
{
  public Guid Id { get; set; }
  public Guid TenantId { get; set; }
  public string Type { get; set; } = string.Empty;
  public string Content { get; set; } = string.Empty;
  public DateTime OccuredOn { get; set; }
  public DateTime? ProcessedOn { get; set; }
  public int AttemptCount { get; set; }
  public DateTime NextAttemptAt { get; set; }

  public static IntegrationOutboxMessage From(IntegrationEvent message)
  {
    if (message.EventId == Guid.Empty || message.TenantId == Guid.Empty)
      throw new InvalidOperationException("Integration events require a stable ID and tenant.");
    return new()
    {
      Id = message.EventId,
      TenantId = message.TenantId,
      Type = message.GetType().FullName!,
      Content = JsonSerializer.Serialize(message, message.GetType()),
      OccuredOn = message.OccurredOn,
      NextAttemptAt = DateTime.UtcNow
    };
  }

  public IntegrationEvent Deserialize()
  {
    // Explicit allowlist also accepts the assembly-qualified names in the legacy Basket outbox.
    Type type = new[] { typeof(BasketCheckoutIntegrationEvent), typeof(ProductPriceChangedIntegrationEvent) }
      .SingleOrDefault(candidate => Type.Split(',')[0] == candidate.FullName)
      ?? throw new InvalidOperationException("Unsupported integration event contract.");
    IntegrationEvent message = (IntegrationEvent?)JsonSerializer.Deserialize(Content, type)
      ?? throw new InvalidOperationException("Invalid integration event payload.");
    if (message.TenantId != Guid.Empty && message.TenantId != TenantId)
      throw new InvalidOperationException("Outbox tenant does not match the event tenant.");
    return message with { EventId = Id, TenantId = TenantId };
  }
}

public static class OutboxPersistenceExtensions
{
  public static void AddIntegrationOutbox(this ModelBuilder builder)
  {
    builder.Entity<IntegrationOutboxMessage>(entity =>
    {
      entity.ToTable("OutboxMessages");
      entity.HasKey(message => message.Id);
      entity.Property(message => message.Type).IsRequired();
      entity.Property(message => message.Content).IsRequired();
      entity.HasIndex(message => new { message.ProcessedOn, message.NextAttemptAt });
    });
  }
}
