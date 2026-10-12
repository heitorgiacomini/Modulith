using System.Diagnostics.Metrics;

namespace Shared.Messaging.Outbox;

public static class OutboxTelemetry
{
  private static readonly Meter Meter = new("Eshop.Application");
  public static readonly Counter<long> Delivered = Meter.CreateCounter<long>("eshop.outbox.delivered");
  public static readonly Counter<long> Failures = Meter.CreateCounter<long>("eshop.outbox.delivery.failures");
}
