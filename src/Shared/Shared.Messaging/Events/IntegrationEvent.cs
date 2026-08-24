namespace Shared.Messaging.Events;

public record IntegrationEvent
{
  public Guid TenantId { get; set; }
  public Guid EventId => Guid.NewGuid();
  public DateTime OccurredOn => DateTime.UtcNow;
  public string EventType => GetType().AssemblyQualifiedName ?? GetType().FullName ?? GetType().Name;
}

