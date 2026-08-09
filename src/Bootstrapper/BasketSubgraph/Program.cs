using Basket;
using Basket.Basket.GraphQL;
using Basket.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Interceptors;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// MediatR satisfies DispatchDomainEventsInterceptor; OutboxProcessor is intentionally excluded
// from the subgraph — it runs in the main Api service which has MassTransit registered.
builder.Services
    .AddMediatRWithAssemblies(typeof(BasketModule).Assembly)
    .AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>()
    .AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>()
    .AddDbContext<BasketDbContext>((sp, o) =>
    {
        o.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        o.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
    });

builder.Services
    .AddGraphQLServer()
    .AddBasketGraphQL()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();
app.MapGraphQL();
await app.RunAsync();
