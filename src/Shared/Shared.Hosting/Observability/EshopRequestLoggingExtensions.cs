using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Shared.Hosting.Observability;

public static class EshopRequestLoggingExtensions
{
  public static IApplicationBuilder UseEshopRequestLogging(this IApplicationBuilder app)
  {
    ArgumentNullException.ThrowIfNull(app);
    return app.UseMiddleware<EshopRequestLoggingMiddleware>();
  }
}

internal sealed class EshopRequestLoggingMiddleware(
  RequestDelegate next,
  ILogger<EshopRequestLoggingMiddleware> logger)
{
  public async Task InvokeAsync(HttpContext context)
  {
    long startedAt = Stopwatch.GetTimestamp();
    bool requestFailed = false;

    try
    {
      await next(context);
    }
    catch
    {
      requestFailed = true;
      throw;
    }
    finally
    {
      int statusCode = requestFailed && context.Response.StatusCode < 500
        ? StatusCodes.Status500InternalServerError
        : context.Response.StatusCode;
      string requestMethod = context.Request.Method;
      string requestPath = context.Request.Path.Value ?? "/";
      string routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText
        ?? requestPath;
      double elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

      LogCompletedRequest(
        logger,
        requestMethod,
        requestPath,
        routePattern,
        statusCode,
        elapsedMilliseconds);
    }
  }

  private static void LogCompletedRequest(
    ILogger logger,
    string requestMethod,
    string requestPath,
    string routePattern,
    int statusCode,
    double elapsedMilliseconds)
  {
    const string MessageTemplate =
      "HTTP {RequestMethod} {RoutePattern} responded {StatusCode} in {ElapsedMilliseconds:F2} ms (path: {RequestPath})";

    if (statusCode >= StatusCodes.Status500InternalServerError)
    {
      logger.LogError(MessageTemplate, requestMethod, routePattern, statusCode, elapsedMilliseconds, requestPath);
      return;
    }

    if (statusCode >= StatusCodes.Status400BadRequest)
    {
      logger.LogWarning(MessageTemplate, requestMethod, routePattern, statusCode, elapsedMilliseconds, requestPath);
      return;
    }

    logger.LogInformation(MessageTemplate, requestMethod, routePattern, statusCode, elapsedMilliseconds, requestPath);
  }
}
