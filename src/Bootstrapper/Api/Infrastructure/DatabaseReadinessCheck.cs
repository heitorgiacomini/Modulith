using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shared.Data.Auditing;

namespace Api.Infrastructure;

public sealed class DatabaseReadinessCheck<TContext>(TContext db) : IHealthCheck where TContext : DbContext
{
  public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
  {
    try
    {
      // Verify runtime privileges and the migrated schema, not merely TCP connectivity.
      await db.Set<AuditRecord>().AsNoTracking().AnyAsync(cancellationToken);
      return HealthCheckResult.Healthy();
    }
    catch (Exception)
    {
      return HealthCheckResult.Unhealthy("Module database is unavailable or not migrated.");
    }
  }
}
