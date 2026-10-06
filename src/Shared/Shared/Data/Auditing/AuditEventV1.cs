using System.Text.Json.Serialization;

namespace Shared.Data.Auditing;

public enum AuditCategory
{
  DataChange,
  BusinessAction,
  SensitiveRead,
  Security,
  Administration
}

public enum AuditOutcome
{
  Succeeded,
  Failed,
  Denied
}

public sealed record AuditActorV1(Guid? Id);

public sealed record AuditSubjectV1(string Type, string Id);

public sealed record AuditValueChangeV1(object? OldValue, object? NewValue);

public sealed record AuditEventV1
{
  public const int CurrentSchemaVersion = 1;

  public required Guid EventId { get; init; }
  public int SchemaVersion { get; init; } = CurrentSchemaVersion;
  public required DateTimeOffset OccurredAtUtc { get; init; }
  public required string Service { get; init; }
  public required string Module { get; init; }

  [JsonConverter(typeof(JsonStringEnumConverter<AuditCategory>))]
  public required AuditCategory Category { get; init; }

  public required string Action { get; init; }

  [JsonConverter(typeof(JsonStringEnumConverter<AuditOutcome>))]
  public required AuditOutcome Outcome { get; init; }

  public Guid? TenantId { get; init; }
  public required AuditActorV1 Actor { get; init; }
  public required AuditSubjectV1 Subject { get; init; }
  public string? TraceId { get; init; }
  public string? SpanId { get; init; }
  public string? CorrelationId { get; init; }
  public IReadOnlyDictionary<string, AuditValueChangeV1> Changes { get; init; } =
    new Dictionary<string, AuditValueChangeV1>();
  public IReadOnlyDictionary<string, object?> Metadata { get; init; } =
    new Dictionary<string, object?>();
}
