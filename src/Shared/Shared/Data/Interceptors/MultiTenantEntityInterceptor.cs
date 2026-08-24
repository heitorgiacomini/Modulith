using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.MultiTenancy;
using Shared.DDD;

namespace Shared.Data.Interceptors;

public sealed class MultiTenantEntityInterceptor(ICurrentTenant currentTenant) : SaveChangesInterceptor
{
  public override InterceptionResult<int> SavingChanges(
    DbContextEventData eventData,
    InterceptionResult<int> result)
  {
    ApplyTenantOwnership(eventData.Context);
    return base.SavingChanges(eventData, result);
  }

  public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
    DbContextEventData eventData,
    InterceptionResult<int> result,
    CancellationToken cancellationToken = default)
  {
    ApplyTenantOwnership(eventData.Context);
    return base.SavingChangesAsync(eventData, result, cancellationToken);
  }

  private void ApplyTenantOwnership(DbContext? context)
  {
    if (context is null)
    {
      return;
    }

    context.ChangeTracker.DetectChanges();
    foreach (EntityEntry<IMultiTenant> entry in context.ChangeTracker.Entries<IMultiTenant>())
    {
      if (entry.State is EntityState.Detached or EntityState.Unchanged)
      {
        continue;
      }

      Guid tenantId = currentTenant.Id ?? throw new InvalidOperationException(
        $"An active tenant is required to save {entry.Metadata.ClrType.Name}.");

      if (entry.State == EntityState.Added && entry.Entity.TenantId is null)
      {
        entry.Entity.TenantId = tenantId;
      }

      if (entry.Entity.TenantId != tenantId)
      {
        throw new InvalidOperationException(
          $"{entry.Metadata.ClrType.Name} belongs to a different tenant.");
      }

      var tenantProperty = entry.Property(entity => entity.TenantId);
      if (entry.State != EntityState.Added && tenantProperty.IsModified)
      {
        throw new InvalidOperationException(
          $"The tenant ownership of {entry.Metadata.ClrType.Name} cannot be changed.");
      }
    }
  }
}
