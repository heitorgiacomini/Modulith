using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Filtering;
using Shared.DDD;

namespace Shared.Data;

public static class SoftDeleteModelBuilderExtensions
{
  public static ModelBuilder ApplySoftDeleteQueryFilters(
    this ModelBuilder modelBuilder,
    IDataFilterContext filterContext)
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
      MemberExpression filterEnabled = Expression.Property(
        Expression.Constant(filterContext),
        nameof(IDataFilterContext.IsSoftDeleteFilterEnabled));
      BinaryExpression filterBody = Expression.OrElse(
        Expression.Not(filterEnabled),
        Expression.Equal(isDeleted, Expression.Constant(false)));
      LambdaExpression filter = Expression.Lambda(filterBody, parameter);
      entityType.SetQueryFilter(filter);
    }

    return modelBuilder;
  }
}
