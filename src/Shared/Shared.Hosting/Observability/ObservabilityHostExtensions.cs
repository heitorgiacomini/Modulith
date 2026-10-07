using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Behaviors;

namespace Shared.Hosting.Observability;

public static class ObservabilityHostExtensions
{
  public static THostApplicationBuilder AddEshopObservability<THostApplicationBuilder>(
    this THostApplicationBuilder builder,
    string defaultServiceName)
    where THostApplicationBuilder : IHostApplicationBuilder
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(defaultServiceName);

    Activity.DefaultIdFormat = ActivityIdFormat.W3C;
    Activity.ForceDefaultIdFormat = true;

    string serviceName =
      builder.Configuration["OTEL_SERVICE_NAME"] ??
      builder.Configuration["OpenTelemetry:ServiceName"] ??
      defaultServiceName;
    string serviceVersion =
      Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
      Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ??
      "unknown";
    string environmentName =
      builder.Configuration["OTEL_DEPLOYMENT_ENVIRONMENT"] ??
      builder.Environment.EnvironmentName;
    string instanceId =
      builder.Configuration["OTEL_SERVICE_INSTANCE_ID"] ??
      $"{Environment.MachineName}:{Environment.ProcessId}";
    bool exportLogs = builder.Configuration.GetValue("OpenTelemetry:ExportLogs", true);

    builder.Logging.ClearProviders();
    _ = builder.Logging.AddJsonConsole(options =>
    {
      options.IncludeScopes = true;
      options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffK";
      options.UseUtcTimestamp = true;
      options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
    });
    _ = builder.Logging.Configure(options =>
      options.ActivityTrackingOptions =
        ActivityTrackingOptions.TraceId |
        ActivityTrackingOptions.SpanId |
        ActivityTrackingOptions.ParentId);

    var openTelemetry = builder.Services
      .AddOpenTelemetry()
      .ConfigureResource(resource => resource
        .AddService(serviceName, serviceVersion: serviceVersion, serviceInstanceId: instanceId)
        .AddAttributes(
        [
          new KeyValuePair<string, object>("deployment.environment.name", environmentName),
          new KeyValuePair<string, object>("host.name", Environment.MachineName),
          new KeyValuePair<string, object>("process.pid", Environment.ProcessId)
        ]));

    if (exportLogs)
    {
      _ = openTelemetry.WithLogging(
        logging => logging.AddOtlpExporter(),
        options =>
        {
          options.IncludeFormattedMessage = true;
          options.IncludeScopes = true;
          options.ParseStateValues = false;
        });
    }

    _ = openTelemetry
      .WithTracing(tracing => tracing
        .AddSource(
          EshopTelemetry.SourceName,
          ApplicationTelemetry.SourceName,
          "MassTransit",
          "Microsoft.EntityFrameworkCore")
        .AddAspNetCoreInstrumentation(options => options.RecordException = true)
        .AddHttpClientInstrumentation(options => options.RecordException = true)
        .AddNpgsql()
        .AddOtlpExporter())
      .WithMetrics(metrics => metrics
        .AddMeter(
          EshopTelemetry.MeterName,
          "MassTransit",
          "Microsoft.AspNetCore.Hosting",
          "Microsoft.AspNetCore.Server.Kestrel",
          "System.Net.Http")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsqlInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

    return builder;
  }
}
