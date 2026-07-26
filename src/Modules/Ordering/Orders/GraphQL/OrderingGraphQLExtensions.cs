using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ordering.Orders.GraphQL;

public static class OrderingGraphQLExtensions
{
  public static IRequestExecutorBuilder AddOrderingGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder.AddTypeExtension<OrderingQueries>();
  }
}
