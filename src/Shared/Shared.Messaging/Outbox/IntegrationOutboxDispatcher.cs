using Shared.Messaging.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Data.MultiTenancy;

namespace Shared.Messaging.Outbox;

// PostgreSQL row locks coordinate replicas. Delivery is at-least-once with a stable MessageId.
public sealed class IntegrationOutboxDispatcher<TContext>(
  IServiceScopeFactory scopes, IBus bus, ILogger<IntegrationOutboxDispatcher<TContext>> logger)
  : BackgroundService where TContext : DbContext
{
  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));
    do
    {
      try { await DispatchBatchAsync(stoppingToken); }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
      catch (Exception exception) { logger.LogError(exception, "Outbox delivery failed for {Module}", typeof(TContext).Name); }
    } while (await timer.WaitForNextTickAsync(stoppingToken));
  }

  public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
  {
    using IServiceScope scope = scopes.CreateScope();
    TContext db = scope.ServiceProvider.GetRequiredService<TContext>();
    string schema = db.Model.GetDefaultSchema() ?? throw new InvalidOperationException("Module schema required.");
    if (schema is not ("catalog" or "basket"))
      throw new InvalidOperationException("Unsupported outbox schema.");
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
    string sql = $"SELECT * FROM \"{schema}\".\"OutboxMessages\" WHERE \"ProcessedOn\" IS NULL AND \"NextAttemptAt\" <= now() ORDER BY \"OccuredOn\", \"Id\" LIMIT 20 FOR UPDATE SKIP LOCKED";
    var messages = await db.Set<IntegrationOutboxMessage>().FromSqlRaw(sql)
      .ToListAsync(cancellationToken);
    foreach (IntegrationOutboxMessage message in messages)
    {
      try
      {
        IntegrationEvent payload = message.Deserialize();
        using IDisposable tenantScope = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(message.TenantId);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await bus.Publish(payload, payload.GetType(), context => context.MessageId = message.Id, timeout.Token);
        message.ProcessedOn = DateTime.UtcNow;
        OutboxTelemetry.Delivered.Add(1, new KeyValuePair<string, object?>("module", schema));
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
      catch (Exception exception)
      {
        message.AttemptCount++;
        OutboxTelemetry.Failures.Add(1, new KeyValuePair<string, object?>("module", schema));
        message.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(message.AttemptCount, 9))));
        // Never log Content: checkout payloads contain personal and payment data.
        logger.LogWarning("Outbox {EventId} attempt {Attempt} failed ({ErrorType})", message.Id, message.AttemptCount, exception.GetType().Name);
      }
    }
    await db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    return messages.Count;
  }
}

