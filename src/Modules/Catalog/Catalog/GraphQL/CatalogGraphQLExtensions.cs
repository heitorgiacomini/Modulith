using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.GraphQL;

public static class CatalogGraphQLExtensions
{
  public static IRequestExecutorBuilder AddCatalogGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder
      .AddSourceSchemaDefaults()
      .AddQueryType(descriptor => descriptor.Name(OperationTypeNames.Query))
      .AddType<ProductListItemType>()
      .AddTypeExtension<CollectionSegmentInfoTypeExtension>()
      .AddTypeExtension<CatalogQueries>();
  }
}

public sealed class CollectionSegmentInfoTypeExtension : ObjectTypeExtension
{
  protected override void Configure(IObjectTypeDescriptor descriptor)
  {
    descriptor.Name("CollectionSegmentInfo");
    descriptor.Shareable();
  }
}
