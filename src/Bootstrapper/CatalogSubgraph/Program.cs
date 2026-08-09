using Catalog;
using Catalog.Data;
using Catalog.GraphQL;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Interceptors;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// MediatR satisfies both DispatchDomainEventsInterceptor and ISender used in CatalogQueries.Product
builder.Services
    .AddMediatRWithAssemblies(typeof(CatalogModule).Assembly)
    .AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>()
    .AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>()
    .AddDbContext<CatalogDbContext>((sp, o) =>
    {
        o.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
        o.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
    });

builder.Services
    .AddGraphQLServer()
    .AddCatalogGraphQL()
    .AddFiltering()
    .AddSorting()
    .ModifyCostOptions(o => o.MaxFieldCost = 5_000);

var app = builder.Build();
app.MapGraphQL();
await app.RunAsync();
