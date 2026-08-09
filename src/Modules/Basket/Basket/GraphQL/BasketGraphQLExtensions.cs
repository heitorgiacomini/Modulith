using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace Basket.Basket.GraphQL;

public static class BasketGraphQLExtensions
{
  public static IRequestExecutorBuilder AddBasketGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder
      .AddSourceSchemaDefaults()
      .AddQueryType(descriptor => descriptor.Name(OperationTypeNames.Query))
      .AddType<ProductReferenceType>()
      .AddTypeExtension<CollectionSegmentInfoTypeExtension>()
      .AddTypeExtension<BasketQueries>();
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
