using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Exceptions.Handler;
using Shared.Hosting.Observability;
using Xunit;

namespace SharedTests;

public sealed class ObservabilityLoggingTests
{
  [Theory]
  [InlineData(200, LogLevel.Information)]
  [InlineData(404, LogLevel.Warning)]
  [InlineData(500, LogLevel.Error)]
  public async Task Request_logging_emits_one_structured_completion_event(
    int statusCode,
    LogLevel expectedLevel)
  {
    CapturingLoggerProvider logs = new();
    await using WebApplication app = CreateRequestLoggingApp(logs, statusCode);
    await app.StartAsync();
    using HttpClient client = app.GetTestClient();
    using HttpRequestMessage request = new(HttpMethod.Get, "/orders/42?token=secret-value");
    request.Headers.Authorization = new("Bearer", "secret-value");

    using HttpResponseMessage response = await client.SendAsync(request);

    Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
    CapturedLog completion = Assert.Single(
      logs.Entries,
      entry => entry.Category.EndsWith("EshopRequestLoggingMiddleware", StringComparison.Ordinal));
    Assert.Equal(expectedLevel, completion.Level);
    Assert.Equal("GET", completion.Attributes["RequestMethod"]);
    Assert.Equal("/orders/{id}", completion.Attributes["RoutePattern"]);
    Assert.Equal("/orders/42", completion.Attributes["RequestPath"]);
    Assert.Equal(statusCode, completion.Attributes["StatusCode"]);
    Assert.True(Convert.ToDouble(completion.Attributes["ElapsedMilliseconds"]) >= 0);
    Assert.DoesNotContain("secret-value", completion.Message, StringComparison.Ordinal);
    Assert.DoesNotContain(
      completion.Attributes.Values,
      value => value?.ToString()?.Contains("secret-value", StringComparison.Ordinal) == true);
  }

  [Fact]
  public async Task Exception_handler_logs_client_exception_and_returns_activity_trace_id()
  {
    CapturingLoggerProvider logs = new();
    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
    CustomExceptionHandler handler = new(loggerFactory.CreateLogger<CustomExceptionHandler>());
    DefaultHttpContext context = CreateHttpContext();
    ValidationException exception = new("The request is invalid.");
    using Activity activity = StartActivity();

    bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    CapturedLog log = Assert.Single(logs.Entries);
    Assert.Equal(LogLevel.Warning, log.Level);
    Assert.Same(exception, log.Exception);
    Assert.Equal(activity.TraceId.ToString(), await ReadTraceIdAsync(context.Response));
  }

  [Fact]
  public async Task Exception_handler_logs_server_exception_at_error_level()
  {
    CapturingLoggerProvider logs = new();
    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(logs));
    CustomExceptionHandler handler = new(loggerFactory.CreateLogger<CustomExceptionHandler>());
    DefaultHttpContext context = CreateHttpContext();
    InternalServerException exception = new("The request failed.");

    bool handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
    CapturedLog log = Assert.Single(logs.Entries);
    Assert.Equal(LogLevel.Error, log.Level);
    Assert.Same(exception, log.Exception);
  }

  private static WebApplication CreateRequestLoggingApp(
    CapturingLoggerProvider logs,
    int statusCode)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      EnvironmentName = "Testing",
    });
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    _ = builder.Logging.AddProvider(logs);

    WebApplication app = builder.Build();
    _ = app.UseEshopRequestLogging();
    _ = app.MapGet("/orders/{id}", (HttpContext context) =>
    {
      context.Response.StatusCode = statusCode;
      return Task.CompletedTask;
    });
    return app;
  }

  private static DefaultHttpContext CreateHttpContext()
  {
    DefaultHttpContext context = new();
    context.Request.Path = "/test";
    context.Response.Body = new MemoryStream();
    return context;
  }

  private static Activity StartActivity()
  {
    Activity activity = new("test-request");
    activity.SetIdFormat(ActivityIdFormat.W3C);
    _ = activity.Start();
    return activity;
  }

  private static async Task<string?> ReadTraceIdAsync(HttpResponse response)
  {
    response.Body.Position = 0;
    using JsonDocument problem = await JsonDocument.ParseAsync(response.Body);
    return problem.RootElement.GetProperty("traceId").GetString();
  }

  private sealed class CapturingLoggerProvider : ILoggerProvider
  {
    public ConcurrentQueue<CapturedLog> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose()
    {
    }
  }

  private sealed class CapturingLogger(
    string category,
    ConcurrentQueue<CapturedLog> entries) : ILogger
  {
    public IDisposable? BeginScope<TState>(TState state)
      where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      Dictionary<string, object?> attributes = state is IEnumerable<KeyValuePair<string, object?>> values
        ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
        : [];
      entries.Enqueue(new(category, logLevel, formatter(state, exception), exception, attributes));
    }
  }

  private sealed record CapturedLog(
    string Category,
    LogLevel Level,
    string Message,
    Exception? Exception,
    IReadOnlyDictionary<string, object?> Attributes);
}
