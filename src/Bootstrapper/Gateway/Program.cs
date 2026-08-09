var builder = WebApplication.CreateBuilder(args);

var schemaPath = builder.Configuration["Fusion:SchemaPath"] ?? "gateway.fgx";

builder.Services
    .AddFusionGatewayServer()
    .ConfigureFromFile(schemaPath);

var app = builder.Build();
app.MapGraphQL();
await app.RunAsync();
