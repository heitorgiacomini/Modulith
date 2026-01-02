using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Shared.Behaviors;

/// <summary>
/// A MediatR pipeline behavior that logs the start and end of request handling,
/// measures the time taken to handle the request and emits a warning when the
/// handling time exceeds a configured threshold.
/// </summary>
/// <typeparam name="TRequest">The request type. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The response type returned by the request handler.</typeparam>
public class LoggingBehavior<TRequest, TResponse>
  (ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull, IRequest<TResponse>
  where TResponse : notnull
{
  /// <summary>
  /// Handles the incoming request by:
  /// 1. Logging the start of handling with request metadata.
  /// 2. Measuring the time taken to invoke the next handler in the pipeline.
  /// 3. Logging a performance warning if handling takes longer than the threshold.
  /// 4. Logging the end of handling and returning the response.
  /// </summary>
  /// <param name="request">The incoming request instance.</param>
  /// <param name="next">Delegate to the next handler in the pipeline.</param>
  /// <param name="cancellationToken">Cancellation token provided by MediatR.</param>
  /// <returns>The response returned by the next handler.</returns>
  public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
  {
    // Log the start of the request handling. Include request and response type names for easier tracing.
    logger.LogInformation(
        "[START] Handle request={Request} - Response={Response} - RequestData={RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

    // Start a stopwatch to measure performance of the handler pipeline.
    Stopwatch timer = new ();
    timer.Start();

    // Invoke the next behavior/handler in the pipeline.
    // Note: MediatR's RequestHandlerDelegate does not accept a CancellationToken parameter here,
    // so cancellation must be observed inside the handler itself if required.
    TResponse response = await next();

    // Stop the timer and calculate elapsed time.
    timer.Stop();
    TimeSpan timeTaken = timer.Elapsed;

    // If the request took more than 3 seconds (using TotalSeconds for an accurate measurement),
    // log a performance warning to help identify slow handlers.
    if (timeTaken.TotalSeconds > 3)
    {
      logger.LogWarning(
          "[PERFORMANCE] The request {Request} took {TimeTakenSeconds} seconds.",
          typeof(TRequest).Name, timeTaken.TotalSeconds);
    }

    // Log the completion of request handling.
    logger.LogInformation("[END] Handled {Request} with {Response}", typeof(TRequest).Name, typeof(TResponse).Name);

    // Return the response from the handler pipeline.
    return response;
  }
}
