using HotChocolate;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.GraphQL;

public static class CatalogGraphQLExtensions
{
  public static IRequestExecutorBuilder AddCatalogGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder
      .AddQueryType(descriptor => descriptor.Name(OperationTypeNames.Query))
      .AddTypeExtension<CatalogQueries>();
  }
}
