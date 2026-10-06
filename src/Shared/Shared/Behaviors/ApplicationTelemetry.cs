using System.Diagnostics;

namespace Shared.Behaviors;

/// <summary>
/// Defines application-owned OpenTelemetry instrumentation sources.
/// </summary>
public static class ApplicationTelemetry
{
  /// <summary>
  /// The name registered with the OpenTelemetry tracing provider.
  /// </summary>
  public const string SourceName = "Eshop.MediatR";

  /// <summary>
  /// Gets the source used for MediatR internal activities.
  /// </summary>
  public static readonly ActivitySource ActivitySource = new (SourceName);
}
