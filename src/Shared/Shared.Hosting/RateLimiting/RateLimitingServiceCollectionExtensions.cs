using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared.Hosting.RateLimiting;

public static class RateLimitingServiceCollectionExtensions
{
  public static IServiceCollection AddApplicationRateLimiting(
    this IServiceCollection services,
    IConfiguration configuration,
    string corsPolicyName)
  {
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configuration);
    ArgumentException.ThrowIfNullOrWhiteSpace(corsPolicyName);

    _ = services
      .AddOptions<ApplicationRateLimitingOptions>()
      .Bind(configuration.GetSection(ApplicationRateLimitingOptions.SectionName))
      .Validate(
        options => options.PermitLimit > 0,
        $"{ApplicationRateLimitingOptions.SectionName}:PermitLimit must be greater than zero.")
      .Validate(
        options => options.WindowSeconds > 0,
        $"{ApplicationRateLimitingOptions.SectionName}:WindowSeconds must be greater than zero.")
      .ValidateOnStart();

    _ = services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
      options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
      {
        ApplicationRateLimitingOptions settings = context.RequestServices
          .GetRequiredService<IOptions<ApplicationRateLimitingOptions>>()
          .Value;

        return RateLimitPartition.GetFixedWindowLimiter(
          GetPartitionKey(context),
          _ => new FixedWindowRateLimiterOptions
          {
            AutoReplenishment = false,
            PermitLimit = settings.PermitLimit,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
          });
      });
      options.OnRejected = (context, cancellationToken) =>
        WriteRejectionAsync(context, corsPolicyName, cancellationToken);
    });

    return services;
  }

  private static string GetPartitionKey(HttpContext context)
  {
    string? subject = context.User.Identity?.IsAuthenticated == true
      ? context.User.FindFirstValue("sub")
      : null;

    if (!string.IsNullOrWhiteSpace(subject))
    {
      return $"user:{subject}";
    }

    IPAddress? address = context.Connection.RemoteIpAddress;
    if (address is null)
    {
      return "anonymous:unknown";
    }

    if (address.IsIPv4MappedToIPv6)
    {
      address = address.MapToIPv4();
    }

    return $"ip:{address}";
  }

  private static async ValueTask WriteRejectionAsync(
    OnRejectedContext context,
    string corsPolicyName,
    CancellationToken cancellationToken)
  {
    HttpResponse response = context.HttpContext.Response;
    response.StatusCode = StatusCodes.Status429TooManyRequests;

    ICorsPolicyProvider policyProvider = context.HttpContext.RequestServices
      .GetRequiredService<ICorsPolicyProvider>();
    CorsPolicy? policy = await policyProvider.GetPolicyAsync(context.HttpContext, corsPolicyName);
    if (policy is not null)
    {
      ICorsService corsService = context.HttpContext.RequestServices
        .GetRequiredService<ICorsService>();
      CorsResult corsResult = corsService.EvaluatePolicy(context.HttpContext, policy);
      corsService.ApplyResult(corsResult, response);
    }

    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
    {
      int retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
      response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
    }

    ProblemDetails problem = new()
    {
      Status = StatusCodes.Status429TooManyRequests,
      Title = "Too Many Requests",
      Detail = "The request rate limit has been exceeded. Retry after the indicated interval.",
      Instance = context.HttpContext.Request.Path,
    };
    problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

    await response.WriteAsJsonAsync(
      problem,
      options: null,
      contentType: "application/problem+json",
      cancellationToken: cancellationToken);
  }
}
