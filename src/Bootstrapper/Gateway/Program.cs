using Gateway;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Shared.Hosting.Observability;
using Shared.Hosting.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddEshopObservability("eshop-gateway");

var schemaPath = builder.Configuration["Fusion:SchemaPath"] ?? "gateway.far";
await WaitForSchemaAsync(schemaPath, TimeSpan.FromMinutes(2));
const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationForwardingHandler>();
builder.Services
    .AddHttpClient("fusion")
    .AddHttpMessageHandler<AuthorizationForwardingHandler>();
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
string publicIssuer = builder.Configuration["Keycloak:PublicIssuer"]
    ?? builder.Configuration["Keycloak:public-issuer"]
    ?? throw new InvalidOperationException("Keycloak:public-issuer is required.");
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuer = publicIssuer;
    });
builder.Services.AddAuthorization();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddApplicationRateLimiting(builder.Configuration, FrontendCorsPolicy);
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
            .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200", "http://127.0.0.1:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder
    .AddGraphQLGateway()
    .ModifyRequestOptions(options =>
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddFileSystemConfiguration(schemaPath);

builder.Services.AddHealthChecks();
builder.Services.AddHttpClient("readiness", client => client.Timeout = TimeSpan.FromSeconds(3));
var app = builder.Build();
app.UseEshopRequestLogging();
app.UseAuthentication();
app.UseRateLimiter();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapReverseProxy().RequireAuthorization();
app.MapGraphQL();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapGet("/health/ready", async (IHttpClientFactory clients, CancellationToken token) =>
{
    string address = builder.Configuration["ReverseProxy:Clusters:api:Destinations:primary:Address"] ?? "http://api:8080/";
    try
    {
        using var response = await clients.CreateClient("readiness").GetAsync(new Uri(new Uri(address), "health/ready"), token);
        return response.IsSuccessStatusCode && File.Exists(schemaPath) && new FileInfo(schemaPath).Length > 0
            ? Results.Ok() : Results.StatusCode(503);
    }
    catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
    {
        return Results.StatusCode(503);
    }
}).AllowAnonymous();
await app.RunAsync();

static async Task WaitForSchemaAsync(string schemaPath, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;

    while (!File.Exists(schemaPath) || new FileInfo(schemaPath).Length == 0)
    {
        if (DateTime.UtcNow >= deadline)
        {
            throw new FileNotFoundException(
                $"Fusion schema archive was not generated within {timeout.TotalSeconds} seconds.",
                schemaPath);
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
