using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shared.Hosting.Observability;

public static class EshopTelemetry
{
  public const string SourceName = "Eshop.Application";
  public const string MeterName = "Eshop.Application";

  public static readonly ActivitySource ActivitySource = new(SourceName);
  public static readonly Meter Meter = new(MeterName);

  public static readonly Counter<long> AuditEventsWritten = Meter.CreateCounter<long>(
    "eshop.audit.events.written",
    description: "Number of best-effort audit events written to the local audit trail.");

  public static readonly Counter<long> AuditWriteFailures = Meter.CreateCounter<long>(
    "eshop.audit.write.failures",
    description: "Number of best-effort audit events that could not be written.");
}
