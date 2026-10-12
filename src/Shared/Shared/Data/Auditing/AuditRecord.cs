using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Shared.Data.Auditing;

// Business audit records are immutable. Delivery state lives in a separate table.
public sealed class AuditRecord
{
  public Guid Id { get; set; }
  public Guid? TenantId { get; set; }
  public DateTimeOffset OccurredAtUtc { get; set; }
  public string Payload { get; set; } = string.Empty;

  public static AuditRecord From(AuditEventV1 auditEvent) => new()
  {
    Id = auditEvent.EventId,
    TenantId = auditEvent.TenantId,
    OccurredAtUtc = auditEvent.OccurredAtUtc,
    Payload = JsonSerializer.Serialize(auditEvent)
  };

  public AuditEventV1 ToEvent() => JsonSerializer.Deserialize<AuditEventV1>(Payload)
    ?? throw new InvalidOperationException("Invalid persisted audit record.");
}

public sealed class AuditDelivery
{
  public Guid Id { get; set; }
  public DateTimeOffset? ExportedAtUtc { get; set; }
}

public static class AuditPersistenceExtensions
{
  public static void AddAuditPersistence(this ModelBuilder builder)
  {
    builder.Entity<AuditRecord>(entity =>
    {
      entity.ToTable("AuditRecords");
      entity.HasKey(record => record.Id);
      entity.Property(record => record.Payload).IsRequired();
      entity.HasIndex(record => new { record.TenantId, record.OccurredAtUtc });
    });
    builder.Entity<AuditDelivery>(entity =>
    {
      entity.ToTable("AuditDeliveries");
      entity.HasKey(delivery => delivery.Id);
      entity.HasOne<AuditRecord>().WithOne().HasForeignKey<AuditDelivery>(delivery => delivery.Id);
      entity.HasIndex(delivery => delivery.ExportedAtUtc);
    });
  }
}
