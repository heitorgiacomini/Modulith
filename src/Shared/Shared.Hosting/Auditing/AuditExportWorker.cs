using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Data.Auditing;
using Shared.Hosting.Observability;

namespace Shared.Hosting.Auditing;

// Files/Loki are an at-least-once projection; AuditRecords remain the source of truth.
public sealed class AuditExportWorker<TContext>(
  IServiceScopeFactory scopes, FileAuditTrail files, ILogger<AuditExportWorker<TContext>> logger)
  : BackgroundService where TContext : DbContext
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using PeriodicTimer timer = new(TimeSpan.FromSeconds(5));
    do
    {
      try { await ExportBatchAsync(stoppingToken); }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
      catch (Exception exception)
      {
        EshopTelemetry.AuditWriteFailures.Add(1);
        logger.LogError(exception, "Audit export failed for {Module}", typeof(TContext).Name);
      }
    } while (await timer.WaitForNextTickAsync(stoppingToken));
  }

  public async Task<int> ExportBatchAsync(CancellationToken cancellationToken)
  {
    using IServiceScope scope = scopes.CreateScope();
    TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
    string schema = db.Model.GetDefaultSchema() ?? throw new InvalidOperationException("Module schema required.");
    if (schema is not ("catalog" or "basket" or "ordering" or "accounts"))
      throw new InvalidOperationException("Unsupported audit schema.");
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    string sql = $"SELECT * FROM \"{schema}\".\"AuditDeliveries\" WHERE \"ExportedAtUtc\" IS NULL ORDER BY \"Id\" LIMIT 100 FOR UPDATE SKIP LOCKED";
    var deliveries = await db.Set<AuditDelivery>().FromSqlRaw(sql)
      .ToListAsync(cancellationToken);
    foreach (AuditDelivery delivery in deliveries)
    {
      AuditRecord record = await db.Set<AuditRecord>().SingleAsync(record => record.Id == delivery.Id, cancellationToken);
      files.WriteConfirmed(record.ToEvent());
      delivery.ExportedAtUtc = DateTimeOffset.UtcNow;
    }
    await db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return deliveries.Count;
  }
}
