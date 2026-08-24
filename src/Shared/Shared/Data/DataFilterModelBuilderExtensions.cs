using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Filtering;
using Shared.DDD;

namespace Shared.Data;

public static class DataFilterModelBuilderExtensions
{
  public static ModelBuilder ApplyDataFilters(
    this ModelBuilder modelBuilder,
    IDataFilterContext filterContext)
  {
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      bool softDelete = typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType);
      bool multiTenant = typeof(IMultiTenant).IsAssignableFrom(entityType.ClrType);
      if (!softDelete && !multiTenant)
      {
        continue;
      }

      ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "entity");
      Expression? filterBody = null;

      if (softDelete)
      {
        MethodCallExpression isDeleted = Expression.Call(
          typeof(EF),
          nameof(EF.Property),
          [typeof(bool)],
          parameter,
          Expression.Constant(nameof(ISoftDelete.IsDeleted)));
        MemberExpression filterEnabled = Expression.Property(
          Expression.Constant(filterContext),
          nameof(IDataFilterContext.IsSoftDeleteFilterEnabled));
        filterBody = Expression.OrElse(
          Expression.Not(filterEnabled),
          Expression.Equal(isDeleted, Expression.Constant(false)));
      }

      if (multiTenant)
      {
        MethodCallExpression tenantId = Expression.Call(
          typeof(EF),
          nameof(EF.Property),
          [typeof(Guid?)],
          parameter,
          Expression.Constant(nameof(IMultiTenant.TenantId)));
        MemberExpression filterEnabled = Expression.Property(
          Expression.Constant(filterContext),
          nameof(IDataFilterContext.IsMultiTenantFilterEnabled));
        MemberExpression currentTenantId = Expression.Property(
          Expression.Constant(filterContext),
          nameof(IDataFilterContext.CurrentTenantId));
        BinaryExpression tenantFilter = Expression.OrElse(
          Expression.Not(filterEnabled),
          Expression.Equal(tenantId, currentTenantId));
        filterBody = filterBody is null
          ? tenantFilter
          : Expression.AndAlso(filterBody, tenantFilter);
      }

      LambdaExpression filter = Expression.Lambda(filterBody!, parameter);
      entityType.SetQueryFilter(filter);
    }

    return modelBuilder;
  }
}
