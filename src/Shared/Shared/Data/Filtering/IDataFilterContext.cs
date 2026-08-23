namespace Shared.Data.Filtering;

public interface IDataFilterContext
{
  bool IsSoftDeleteFilterEnabled { get; }
}
