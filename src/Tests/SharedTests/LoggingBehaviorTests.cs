using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Behaviors;
using System.Diagnostics;
using Xunit;

namespace SharedTests;

public sealed class LoggingBehaviorTests
{
  [Fact]
  public async Task Does_not_write_request_values_to_application_logs()
  {
    const string secret = "do-not-export-this-token";
    CapturingLogger<LoggingBehavior<SensitiveRequest, string>> logger = new();
    LoggingBehavior<SensitiveRequest, string> behavior = new(logger);

    string result = await behavior.Handle(
      new SensitiveRequest(secret),
      _ => Task.FromResult("ok"),
      CancellationToken.None);

    Assert.Equal("ok", result);
    Assert.Single(logger.Entries);
    Assert.Contains(nameof(SensitiveRequest), logger.Entries[0].Message);
    Assert.DoesNotContain(logger.Messages, message => message.Contains(secret));
  }

  [Fact]
  public async Task Creates_nested_internal_span_and_one_completion_event()
  {
    using ActivityListener listener = ListenToApplicationActivities();
    using Activity parent = new("request");
    _ = parent.Start();
    CapturingLogger<LoggingBehavior<SensitiveRequest, string>> logger = new();
    LoggingBehavior<SensitiveRequest, string> behavior = new(logger);

    string result = await behavior.Handle(
      new SensitiveRequest("secret"),
      _ => Task.FromResult("ok"),
      CancellationToken.None);

    Assert.Equal("ok", result);
    CapturedLog entry = Assert.Single(logger.Entries);
    Assert.Equal(LogLevel.Information, entry.Level);
    Assert.Null(entry.Exception);
    Assert.Contains("Success", entry.Message);
  }

  [Fact]
  public async Task Records_failure_on_span_and_log_without_request_values()
  {
    const string secret = "do-not-export-this-token";
    using ActivityListener listener = ListenToApplicationActivities();
    Activity? stopped = null;
    listener.ActivityStopped = activity => stopped = activity;
    CapturingLogger<LoggingBehavior<SensitiveRequest, string>> logger = new();
    LoggingBehavior<SensitiveRequest, string> behavior = new(logger);
    InvalidOperationException failure = new("handler failed");

    InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
      behavior.Handle(
        new SensitiveRequest(secret),
        _ => Task.FromException<string>(failure),
        CancellationToken.None));

    Assert.Same(failure, thrown);
    CapturedLog entry = Assert.Single(logger.Entries);
    Assert.Equal(LogLevel.Error, entry.Level);
    Assert.Same(failure, entry.Exception);
    Assert.DoesNotContain(secret, entry.Message, StringComparison.Ordinal);
    Assert.NotNull(stopped);
    Assert.Equal(ActivityStatusCode.Error, stopped.Status);
    Assert.Contains(stopped.Events, activityEvent => activityEvent.Name == "exception");
  }

  private static ActivityListener ListenToApplicationActivities()
  {
    ActivityListener listener = new()
    {
      ShouldListenTo = source => source.Name == ApplicationTelemetry.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
      SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
    };
    ActivitySource.AddActivityListener(listener);
    return listener;
  }

  private sealed record SensitiveRequest(string Token) : IRequest<string>;

  private sealed class CapturingLogger<T> : ILogger<T>
  {
    public List<CapturedLog> Entries { get; } = [];
    public IEnumerable<string> Messages => Entries.Select(entry => entry.Message);

    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter) =>
      Entries.Add(new(logLevel, formatter(state, exception), exception));
  }

  private sealed record CapturedLog(LogLevel Level, string Message, Exception? Exception);
}
