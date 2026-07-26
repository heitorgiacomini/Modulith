using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Basket.Basket.GraphQL;

public static class BasketGraphQLExtensions
{
  public static IRequestExecutorBuilder AddBasketGraphQL(this IRequestExecutorBuilder builder)
  {
    return builder.AddTypeExtension<BasketQueries>();
  }
}
