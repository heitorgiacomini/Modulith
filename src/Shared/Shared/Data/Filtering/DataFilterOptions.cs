namespace Shared.Data.Filtering;

public sealed class DataFilterOptions
{
  public IDictionary<Type, bool> DefaultStates { get; } = new Dictionary<Type, bool>();

  public bool IsEnabled<TFilter>() where TFilter : class =>
    !DefaultStates.TryGetValue(typeof(TFilter), out bool isEnabled) || isEnabled;
}
