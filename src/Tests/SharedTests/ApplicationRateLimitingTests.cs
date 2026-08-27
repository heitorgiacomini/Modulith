using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Hosting.RateLimiting;
using Xunit;

namespace SharedTests;

public sealed class ApplicationRateLimitingTests
{
  private const string TestCorsPolicy = "TestCors";

  [Theory]
  [InlineData(null, null)]
  [InlineData("0", "60")]
  [InlineData("-1", "60")]
  [InlineData("60", null)]
  [InlineData("60", "0")]
  [InlineData("60", "-1")]
  public async Task Startup_rejects_missing_or_non_positive_configuration(
    string? permitLimit,
    string? windowSeconds)
  {
    await using WebApplication app = CreateApp(permitLimit, windowSeconds);

    _ = await Assert.ThrowsAsync<OptionsValidationException>(() => app.StartAsync());
  }

  [Fact]
  public async Task Sixty_first_request_is_rejected_with_problem_details_and_retry_after()
  {
    await using WebApplication app = await StartAppAsync("60", "60");
    using HttpClient client = app.GetTestClient();

    for (int requestNumber = 1; requestNumber <= 60; requestNumber++)
    {
      using HttpResponseMessage accepted = await client.GetAsync("/test");
      Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
      Assert.False(accepted.Headers.Contains("RateLimit-Limit"));
      Assert.False(accepted.Headers.Contains("RateLimit-Remaining"));
      Assert.False(accepted.Headers.Contains("RateLimit-Reset"));
    }

    using HttpRequestMessage rejectedRequest = new(HttpMethod.Get, "/test");
    rejectedRequest.Headers.Add("Origin", "http://localhost:4200");
    using HttpResponseMessage rejected = await client.SendAsync(rejectedRequest);

    Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
    Assert.Equal(
      "http://localhost:4200",
      Assert.Single(rejected.Headers.GetValues("Access-Control-Allow-Origin")));
    Assert.NotNull(rejected.Headers.RetryAfter?.Delta);
    Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);

    using JsonDocument body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
    JsonElement root = body.RootElement;
    Assert.Equal(429, root.GetProperty("status").GetInt32());
    Assert.Equal("Too Many Requests", root.GetProperty("title").GetString());
    Assert.Equal(
      "The request rate limit has been exceeded. Retry after the indicated interval.",
      root.GetProperty("detail").GetString());
    Assert.Equal("/test", root.GetProperty("instance").GetString());
    Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString()));
  }

  [Fact]
  public async Task Authenticated_subject_takes_precedence_over_ip_address()
  {
    await using WebApplication app = await StartAppAsync("1", "60");
    using HttpClient client = app.GetTestClient();

    using HttpResponseMessage first = await SendAsync(client, "user-a", "192.0.2.1");
    using HttpResponseMessage sameUserDifferentIp = await SendAsync(client, "user-a", "192.0.2.2");
    using HttpResponseMessage differentUser = await SendAsync(client, "user-b", "192.0.2.1");

    Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, sameUserDifferentIp.StatusCode);
    Assert.Equal(HttpStatusCode.OK, differentUser.StatusCode);
  }

  [Fact]
  public async Task Anonymous_callers_are_partitioned_by_normalized_ip_address()
  {
    await using WebApplication app = await StartAppAsync("1", "60");
    using HttpClient client = app.GetTestClient();

    using HttpResponseMessage mappedIpv6 = await SendAsync(client, null, "::ffff:192.0.2.1");
    using HttpResponseMessage sameIpv4 = await SendAsync(client, null, "192.0.2.1");
    using HttpResponseMessage differentIp = await SendAsync(client, null, "192.0.2.2");

    Assert.Equal(HttpStatusCode.OK, mappedIpv6.StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, sameIpv4.StatusCode);
    Assert.Equal(HttpStatusCode.OK, differentIp.StatusCode);
  }

  [Fact]
  public async Task Anonymous_callers_without_an_ip_share_the_unknown_partition()
  {
    await using WebApplication app = await StartAppAsync("1", "60");
    using HttpClient client = app.GetTestClient();

    using HttpResponseMessage first = await SendAsync(client, null, null);
    using HttpResponseMessage second = await SendAsync(client, null, null);

    Assert.Equal(HttpStatusCode.OK, first.StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
  }

  [Fact]
  public async Task Options_and_schema_requests_consume_the_same_quota()
  {
    await using WebApplication app = await StartAppAsync("2", "60");
    using HttpClient client = app.GetTestClient();

    using HttpResponseMessage schema = await client.GetAsync("/graphql/catalog/schema.graphqls");
    using HttpRequestMessage preflightRequest = new(HttpMethod.Options, "/test");
    preflightRequest.Headers.Add("Origin", "http://localhost:4200");
    preflightRequest.Headers.Add("Access-Control-Request-Method", "GET");
    using HttpResponseMessage preflight = await client.SendAsync(preflightRequest);
    using HttpResponseMessage rejected = await client.GetAsync("/test");

    Assert.Equal(HttpStatusCode.OK, schema.StatusCode);
    Assert.Equal(HttpStatusCode.NoContent, preflight.StatusCode);
    Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
  }

  private static WebApplication CreateApp(string? permitLimit, string? windowSeconds)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
      EnvironmentName = "Testing",
    });
    builder.WebHost.UseTestServer();
    builder.Configuration.Sources.Clear();
    builder.Logging.ClearProviders();

    Dictionary<string, string?> settings = [];
    if (permitLimit is not null)
    {
      settings["RateLimiting:PermitLimit"] = permitLimit;
    }

    if (windowSeconds is not null)
    {
      settings["RateLimiting:WindowSeconds"] = windowSeconds;
    }

    _ = builder.Configuration.AddInMemoryCollection(settings);
    _ = builder.Services.AddCors(options => options.AddPolicy(
      TestCorsPolicy,
      policy => policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
    _ = builder.Services.AddApplicationRateLimiting(builder.Configuration, TestCorsPolicy);

    WebApplication app = builder.Build();
    _ = app.Use(async (context, next) =>
    {
      if (context.Request.Headers.TryGetValue("X-Test-Subject", out var subject))
      {
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
          [new Claim("sub", subject.ToString())],
          authenticationType: "Test"));
      }

      context.Connection.RemoteIpAddress = context.Request.Headers.TryGetValue(
        "X-Test-Remote-IP",
        out var remoteIp)
        ? IPAddress.Parse(remoteIp.ToString())
        : null;

      await next(context);
    });
    _ = app.UseRateLimiter();
    _ = app.UseCors(TestCorsPolicy);
    _ = app.MapMethods(
      "/{**path}",
      [HttpMethods.Get, HttpMethods.Post, HttpMethods.Options],
      () => Results.Ok());

    return app;
  }

  private static async Task<WebApplication> StartAppAsync(
    string permitLimit,
    string windowSeconds)
  {
    WebApplication app = CreateApp(permitLimit, windowSeconds);
    await app.StartAsync();
    return app;
  }

  private static Task<HttpResponseMessage> SendAsync(
    HttpClient client,
    string? subject,
    string? remoteIp)
  {
    HttpRequestMessage request = new(HttpMethod.Get, "/test");
    if (subject is not null)
    {
      request.Headers.Add("X-Test-Subject", subject);
    }

    if (remoteIp is not null)
    {
      request.Headers.Add("X-Test-Remote-IP", remoteIp);
    }

    return client.SendAsync(request);
  }
}
