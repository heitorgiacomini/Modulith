using Gateway;
using Keycloak.AuthServices.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

var schemaPath = builder.Configuration["Fusion:SchemaPath"] ?? "gateway.far";
await WaitForSchemaAsync(schemaPath, TimeSpan.FromMinutes(2));
const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AuthorizationForwardingHandler>();
builder.Services
    .AddHttpClient("fusion")
    .AddHttpMessageHandler<AuthorizationForwardingHandler>();
builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
string publicIssuer = builder.Configuration["Keycloak:public-issuer"]
    ?? throw new InvalidOperationException("Keycloak:public-issuer is required.");
builder.Services.PostConfigure<JwtBearerOptions>(
    JwtBearerDefaults.AuthenticationScheme,
    options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidIssuer = publicIssuer;
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy
            .WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder
    .AddGraphQLGateway()
    .ModifyRequestOptions(options =>
        options.IncludeExceptionDetails = builder.Environment.IsDevelopment())
    .AddFileSystemConfiguration(schemaPath);

var app = builder.Build();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapGraphQL();
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
