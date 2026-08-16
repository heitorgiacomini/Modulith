var builder = WebApplication.CreateBuilder(args);

var schemaPath = builder.Configuration["Fusion:SchemaPath"] ?? "gateway.far";
const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddHttpClient();
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
app.MapGraphQL();
await app.RunAsync();
