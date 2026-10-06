using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Behaviors;

/// <summary>
/// A MediatR pipeline behavior that creates an internal activity and emits one
/// structured completion event, including elapsed time and outcome.
/// </summary>
/// <typeparam name="TRequest">The request type. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The response type returned by the request handler.</typeparam>
public class LoggingBehavior<TRequest, TResponse>
  (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IRequest<TResponse>
  where TResponse : notnull
{
  private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromSeconds(3);

  /// <summary>
  /// Handles the request inside an internal activity and records one completion
  /// event, using warning severity for slow requests and error severity for failures.
  /// </summary>
  /// <param name="request">The incoming request instance.</param>
  /// <param name="next">Delegate to the next handler in the pipeline.</param>
  /// <param name="cancellationToken">Cancellation token provided by MediatR.</param>
  /// <returns>The response returned by the next handler.</returns>
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    string requestType = typeof(TRequest).Name;
    string responseType = typeof(TResponse).Name;
    using Activity? activity = ApplicationTelemetry.ActivitySource.StartActivity(
      $"MediatR {requestType}",
      ActivityKind.Internal);
    activity?.SetTag("mediatr.request.type", requestType);
    activity?.SetTag("mediatr.response.type", responseType);

    Stopwatch timer = Stopwatch.StartNew();
    try
    {
      // MediatR's delegate does not accept a token here; handlers observe their injected token.
      TResponse response = await next();
      timer.Stop();

      activity?.SetTag("mediatr.outcome", "Success");
      activity?.SetStatus(ActivityStatusCode.Ok);
      LogLevel level = timer.Elapsed >= SlowRequestThreshold
        ? LogLevel.Warning
        : LogLevel.Information;
      logger.Log(
        level,
        "Handled {RequestType} with {ResponseType} in {ElapsedMilliseconds:F2} ms ({Outcome})",
        requestType,
        responseType,
        timer.Elapsed.TotalMilliseconds,
        "Success");
      return response;
    }
    catch (Exception exception)
    {
      timer.Stop();
      activity?.SetTag("mediatr.outcome", "Failure");
      activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
      activity?.AddEvent(new ActivityEvent(
        "exception",
        tags: new ActivityTagsCollection
        {
          ["exception.type"] = exception.GetType().FullName,
          ["exception.message"] = exception.Message,
          ["exception.stacktrace"] = exception.ToString()
        }));
      logger.LogError(
        exception,
        "Failed {RequestType} with {ResponseType} in {ElapsedMilliseconds:F2} ms ({Outcome})",
        requestType,
        responseType,
        timer.Elapsed.TotalMilliseconds,
        "Failure");
      throw;
    }
  }
}
