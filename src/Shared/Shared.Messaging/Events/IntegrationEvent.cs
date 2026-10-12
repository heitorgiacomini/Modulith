namespace Shared.Messaging.Events;

public record IntegrationEvent
{
  public Guid TenantId { get; set; }
  public Guid EventId { get; init; } = Guid.NewGuid();
  public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
  public string EventType => GetType().AssemblyQualifiedName ?? GetType().FullName ?? GetType().Name;
}
