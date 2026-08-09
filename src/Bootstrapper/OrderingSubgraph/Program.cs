using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ordering;
using Ordering.Data;
using Ordering.Orders.GraphQL;
using Shared.Data.Interceptors;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// MediatR satisfies DispatchDomainEventsInterceptor
builder.Services
    .AddMediatRWithAssemblies(typeof(OrderingModule).Assembly)
    .AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>()
    .AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>()
    .AddDbContext<OrderingDbContext>((sp, o) =>
    {
        o.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        o.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
    });

builder.Services
    .AddGraphQLServer()
    .AddOrderingGraphQL()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();
app.MapGraphQL();
await app.RunAsync();
