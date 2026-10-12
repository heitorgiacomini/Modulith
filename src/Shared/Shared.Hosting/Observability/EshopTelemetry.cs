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
    description: "Number of audit events exported to the local observability projection.");

  public static readonly Counter<long> AuditWriteFailures = Meter.CreateCounter<long>(
    "eshop.audit.write.failures",
    description: "Number of failed audit projection writes; committed business audits remain in PostgreSQL.");
}
