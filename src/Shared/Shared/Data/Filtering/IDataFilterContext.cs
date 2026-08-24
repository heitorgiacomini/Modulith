namespace Shared.Data.Filtering;

public interface IDataFilterContext
{
  bool IsSoftDeleteFilterEnabled { get; }
  bool IsMultiTenantFilterEnabled { get; }
  Guid? CurrentTenantId { get; }
}
