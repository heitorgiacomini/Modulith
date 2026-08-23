using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.DDD;

namespace Shared.Data;

public static class SoftDeleteModelBuilderExtensions
{
  public static ModelBuilder ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes()
      .Where(entityType => typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType)))
    {
      ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
      MethodCallExpression isDeleted = Expression.Call(
        typeof(EF),
        nameof(EF.Property),
        [typeof(bool)],
        parameter,
        Expression.Constant(nameof(ISoftDelete.IsDeleted)));
      BinaryExpression filterBody = Expression.Equal(isDeleted, Expression.Constant(false));
      LambdaExpression filter = Expression.Lambda(filterBody, parameter);
      entityType.SetQueryFilter(filter);
    }

    return modelBuilder;
  }
}
