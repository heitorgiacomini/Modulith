using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Orders.GraphQL;

public static class OrderingGraphQLExtensions
{
  public static IRequestExecutorBuilder AddOrderingGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder
      .AddSourceSchemaDefaults()
      .AddQueryType(descriptor => descriptor.Name(OperationTypeNames.Query))
      .AddType<ProductReferenceType>()
      .AddTypeExtension<CollectionSegmentInfoTypeExtension>()
      .AddTypeExtension<OrderingQueries>();
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
