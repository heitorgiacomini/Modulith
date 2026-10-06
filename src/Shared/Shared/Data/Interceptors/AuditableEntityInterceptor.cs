using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Auditing;
using Shared.Data.MultiTenancy;
using Shared.DDD;

namespace Shared.Data.Interceptors;

public sealed class AuditableEntityInterceptor(
  ICurrentUser currentUser,
  ICurrentTenant currentTenant,
  IAuditTrail auditTrail) : SaveChangesInterceptor
{
  private static readonly HashSet<string> AuditPropertyNames =
  [
    nameof(ICreationAuditedObject.CreatedAt), nameof(ICreationAuditedObject.CreatedBy),
    nameof(IModificationAuditedObject.LastModified), nameof(IModificationAuditedObject.LastModifiedBy),
    nameof(ISoftDelete.IsDeleted), nameof(IDeletionAuditedObject.DeletedAt),
    nameof(IDeletionAuditedObject.DeletedBy)
  ];

  private static readonly string[] SensitiveNameFragments =
  [
    "password", "secret", "token", "authorization", "card", "payment", "expiration", "last4",
    "email", "phone", "address", "postal", "body", "query", "document",
    "credential", "apikey", "cookie"
  ]; //, "graphql"

  private readonly ConcurrentDictionary<Guid, IReadOnlyList<PendingAuditEvent>> pendingEvents = [];

  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    PrepareAudit(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    PrepareAudit(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
  {
    WriteAuditEvents(eventData.Context);
    return base.SavedChanges(eventData, result);
  }

  public override ValueTask<int> SavedChangesAsync(
    SaveChangesCompletedEventData eventData,
    int result,
    CancellationToken cancellationToken = default)
  {
    WriteAuditEvents(eventData.Context);
    return base.SavedChangesAsync(eventData, result, cancellationToken);
  }

  public override void SaveChangesFailed(DbContextErrorEventData eventData)
  {
    DiscardAuditEvents(eventData.Context);
    base.SaveChangesFailed(eventData);
  }

  public override Task SaveChangesFailedAsync(
    DbContextErrorEventData eventData,
    CancellationToken cancellationToken = default)
  {
    DiscardAuditEvents(eventData.Context);
    return base.SaveChangesFailedAsync(eventData, cancellationToken);
  }

  public override void SaveChangesCanceled(DbContextEventData eventData)
  {
    DiscardAuditEvents(eventData.Context);
    base.SaveChangesCanceled(eventData);
  }

  public override Task SaveChangesCanceledAsync(
    DbContextEventData eventData,
    CancellationToken cancellationToken = default)
  {
    DiscardAuditEvents(eventData.Context);
    return base.SaveChangesCanceledAsync(eventData, cancellationToken);
  }

  private void PrepareAudit(DbContext? context)
  {
    if (context is null)
    {
      return;
    }

    context.ChangeTracker.DetectChanges();
    DateTimeOffset timestamp = DateTimeOffset.UtcNow;
    Guid? actorId = currentUser.Id;
    List<PendingAuditEvent> events = [];

    foreach (EntityEntry<IEntity> entry in context.ChangeTracker.Entries<IEntity>().ToArray())
    {
      EntityState originalState = entry.State;
      bool hasOwnedChanges = entry.HasChangedOwnedEntities();
      if (originalState is EntityState.Detached or EntityState.Unchanged && !hasOwnedChanges)
      {
        continue;
      }

      string operation = originalState switch
      {
        EntityState.Added => "Created",
        EntityState.Deleted => "Deleted",
        _ => "Modified"
      };
      Dictionary<string, AuditValueChangeV1> changes = CaptureChanges(entry, originalState);

      if (originalState == EntityState.Added)
      {
        if (entry.Entity is ICreationAuditedObject creationAudited)
        {
          creationAudited.CreatedAt = timestamp;
          creationAudited.CreatedBy = actorId;
        }
      }

      if (originalState == EntityState.Deleted && entry.Entity is ISoftDelete softDelete)
      {
        entry.State = EntityState.Modified;
        if (!softDelete.IsDeleted)
        {
          softDelete.IsDeleted = true;
          if (entry.Entity is IDeletionAuditedObject deletionAudited)
          {
            deletionAudited.DeletedAt = timestamp;
            deletionAudited.DeletedBy = actorId;
          }
        }
      }

      if (originalState == EntityState.Modified ||
        originalState == EntityState.Unchanged && hasOwnedChanges)
      {
        if (entry.Entity is IModificationAuditedObject modificationAudited)
        {
          modificationAudited.LastModified = timestamp;
          modificationAudited.LastModifiedBy = actorId;
        }
      }

      if (ShouldLog(entry))
      {
        Activity? activity = Activity.Current;
        events.Add(new PendingAuditEvent(
          context.GetType().Name.Replace("DbContext", string.Empty, StringComparison.Ordinal),
          entry.Metadata.ClrType.Name,
          GetEntityId(entry),
          operation,
          actorId,
          currentTenant.Id,
          timestamp,
          activity?.TraceId.ToString() ?? currentUser.TraceId,
          activity?.SpanId.ToString(),
          activity?.GetBaggageItem("correlation.id"),
          changes));
      }
    }

    if (events.Count == 0)
    {
      pendingEvents.TryRemove(context.ContextId.InstanceId, out _);
    }
    else
    {
      pendingEvents[context.ContextId.InstanceId] = events;
    }
  }

  private static Dictionary<string, AuditValueChangeV1> CaptureChanges(
    EntityEntry<IEntity> entry,
    EntityState state)
  {
    Dictionary<string, AuditValueChangeV1> changes = [];
    foreach (PropertyEntry property in entry.Properties)
    {
      if (property.Metadata.IsPrimaryKey() || AuditPropertyNames.Contains(property.Metadata.Name))
      {
        continue;
      }

      if (state is not (EntityState.Added or EntityState.Deleted) && !property.IsModified)
      {
        continue;
      }

      bool sensitive = IsSensitive(property.Metadata.Name, property.Metadata.PropertyInfo);
      object? oldValue = state == EntityState.Added ? null : property.OriginalValue;
      object? newValue = state == EntityState.Deleted ? null : property.CurrentValue;
      changes[property.Metadata.Name] = sensitive
        ? new AuditValueChangeV1("[REDACTED]", "[REDACTED]")
        : new AuditValueChangeV1(oldValue, newValue);
    }

    CaptureComplexChanges(entry.ComplexProperties, state, changes, null);

    return changes;
  }

  private static void CaptureComplexChanges(
    IEnumerable<ComplexPropertyEntry> complexProperties,
    EntityState state,
    IDictionary<string, AuditValueChangeV1> changes,
    string? parentPath)
  {
    foreach (ComplexPropertyEntry complexProperty in complexProperties)
    {
      string path = string.IsNullOrEmpty(parentPath)
        ? complexProperty.Metadata.Name
        : $"{parentPath}.{complexProperty.Metadata.Name}";

      foreach (PropertyEntry property in complexProperty.Properties)
      {
        if (state is not (EntityState.Added or EntityState.Deleted) && !property.IsModified)
        {
          continue;
        }

        bool sensitive = IsSensitive(property.Metadata.Name, property.Metadata.PropertyInfo) ||
          IsSensitive(path, complexProperty.Metadata.PropertyInfo);
        object? oldValue = state == EntityState.Added ? null : property.OriginalValue;
        object? newValue = state == EntityState.Deleted ? null : property.CurrentValue;
        changes[$"{path}.{property.Metadata.Name}"] = sensitive
          ? new AuditValueChangeV1("[REDACTED]", "[REDACTED]")
          : new AuditValueChangeV1(oldValue, newValue);
      }

      CaptureComplexChanges(complexProperty.ComplexProperties, state, changes, path);
    }
  }

  private static bool IsSensitive(string propertyName, PropertyInfo? propertyInfo) =>
    propertyInfo?.IsDefined(typeof(AuditSensitiveAttribute), true) == true ||
    SensitiveNameFragments.Any(fragment => propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));

  private static bool ShouldLog(EntityEntry<IEntity> entry) => entry.Entity is IAuditedObject;

  private static string GetEntityId(EntityEntry<IEntity> entry)
  {
    PropertyEntry? key = entry.Properties.FirstOrDefault(property => property.Metadata.IsPrimaryKey());
    return Convert.ToString(key?.CurrentValue, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
  }

  private void WriteAuditEvents(DbContext? context)
  {
    if (context is null || !pendingEvents.TryRemove(context.ContextId.InstanceId, out var events))
    {
      return;
    }

    foreach (PendingAuditEvent auditEvent in events)
    {
      auditTrail.Write(new AuditEventV1
      {
        EventId = Guid.NewGuid(),
        OccurredAtUtc = auditEvent.TimestampUtc,
        Service = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ??
          AppDomain.CurrentDomain.FriendlyName,
        Module = auditEvent.Module,
        Category = AuditCategory.DataChange,
        Action = auditEvent.Operation,
        Outcome = AuditOutcome.Succeeded,
        TenantId = auditEvent.TenantId,
        Actor = new AuditActorV1(auditEvent.ActorId),
        Subject = new AuditSubjectV1(auditEvent.EntityType, auditEvent.EntityId),
        TraceId = auditEvent.TraceId,
        SpanId = auditEvent.SpanId,
        CorrelationId = auditEvent.CorrelationId,
        Changes = auditEvent.Changes
      });
    }
  }

  private void DiscardAuditEvents(DbContext? context)
  {
    if (context is not null)
    {
      pendingEvents.TryRemove(context.ContextId.InstanceId, out _);
    }
  }

  private sealed record PendingAuditEvent(
    string Module,
    string EntityType,
    string EntityId,
    string Operation,
    Guid? ActorId,
    Guid? TenantId,
    DateTimeOffset TimestampUtc,
    string? TraceId,
    string? SpanId,
    string? CorrelationId,
    IReadOnlyDictionary<string, AuditValueChangeV1> Changes);
}

public static class Extensions
{
  public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
    entry.References.Any(reference =>
      reference.TargetEntry is not null &&
      reference.TargetEntry.Metadata.IsOwned() &&
      reference.TargetEntry.State is EntityState.Added or EntityState.Modified);
}
