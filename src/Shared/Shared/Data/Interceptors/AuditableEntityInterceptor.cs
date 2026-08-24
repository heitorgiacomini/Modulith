using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Data.Auditing;
using Shared.Data.MultiTenancy;
using Shared.DDD;

namespace Shared.Data.Interceptors;

public sealed class AuditableEntityInterceptor(
  ICurrentUser currentUser,
  ICurrentTenant currentTenant,
  ILogger<AuditableEntityInterceptor> logger) : SaveChangesInterceptor
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
    "email", "phone", "address", "postal"
  ];

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
      Dictionary<string, AuditValueChange> changes = CaptureChanges(entry, originalState);

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
        events.Add(new PendingAuditEvent(
          context.GetType().Name.Replace("DbContext", string.Empty, StringComparison.Ordinal),
          entry.Metadata.ClrType.Name,
          GetEntityId(entry),
          operation,
          actorId,
          currentTenant.Id,
          timestamp,
          currentUser.TraceId,
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

  private static Dictionary<string, AuditValueChange> CaptureChanges(
    EntityEntry<IEntity> entry,
    EntityState state)
  {
    Dictionary<string, AuditValueChange> changes = [];
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
        ? new AuditValueChange("[REDACTED]", "[REDACTED]")
        : new AuditValueChange(oldValue, newValue);
    }

    CaptureComplexChanges(entry.ComplexProperties, state, changes, null);

    return changes;
  }

  private static void CaptureComplexChanges(
    IEnumerable<ComplexPropertyEntry> complexProperties,
    EntityState state,
    IDictionary<string, AuditValueChange> changes,
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
          ? new AuditValueChange("[REDACTED]", "[REDACTED]")
          : new AuditValueChange(oldValue, newValue);
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
      logger.LogInformation(
        "Business entity {AuditOperation}: {AuditModule}.{AuditEntityType} {AuditEntityId} by {AuditActorId} in tenant {AuditTenantId} at {AuditTimestampUtc}. Trace: {AuditTraceId}. Changes: {AuditChanges}",
        auditEvent.Operation,
        auditEvent.Module,
        auditEvent.EntityType,
        auditEvent.EntityId,
        auditEvent.ActorId,
        auditEvent.TenantId,
        auditEvent.TimestampUtc,
        auditEvent.TraceId,
        JsonSerializer.Serialize(auditEvent.Changes));
    }
  }

  private void DiscardAuditEvents(DbContext? context)
  {
    if (context is not null)
    {
      pendingEvents.TryRemove(context.ContextId.InstanceId, out _);
    }
  }

  private sealed record AuditValueChange(object? OldValue, object? NewValue);

  private sealed record PendingAuditEvent(
    string Module,
    string EntityType,
    string EntityId,
    string Operation,
    Guid? ActorId,
    Guid? TenantId,
    DateTimeOffset TimestampUtc,
    string? TraceId,
    IReadOnlyDictionary<string, AuditValueChange> Changes);
}

public static class Extensions
{
  public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
    entry.References.Any(reference =>
      reference.TargetEntry is not null &&
      reference.TargetEntry.Metadata.IsOwned() &&
      reference.TargetEntry.State is EntityState.Added or EntityState.Modified);
}
