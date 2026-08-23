using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shared.Data.Filtering;

public sealed class DataFilter(IServiceProvider serviceProvider) : IDataFilter
{
  private readonly ConcurrentDictionary<Type, object> filters = [];

  public IDisposable Enable<TFilter>() where TFilter : class => GetFilter<TFilter>().Enable();

  public IDisposable Disable<TFilter>() where TFilter : class => GetFilter<TFilter>().Disable();

  public bool IsEnabled<TFilter>() where TFilter : class => GetFilter<TFilter>().IsEnabled;

  private IDataFilter<TFilter> GetFilter<TFilter>() where TFilter : class =>
    (IDataFilter<TFilter>)filters.GetOrAdd(
      typeof(TFilter),
      _ => serviceProvider.GetRequiredService<IDataFilter<TFilter>>());
}

public sealed class DataFilter<TFilter>(IOptions<DataFilterOptions> options) : IDataFilter<TFilter>
  where TFilter : class
{
  private readonly AsyncLocal<bool?> state = new();

  public bool IsEnabled => state.Value ?? options.Value.IsEnabled<TFilter>();

  public IDisposable Enable() => Change(true);

  public IDisposable Disable() => Change(false);

  private IDisposable Change(bool isEnabled)
  {
    bool? previousState = state.Value;
    state.Value = isEnabled;
    return new RestoreFilterState(() => state.Value = previousState);
  }

  private sealed class RestoreFilterState(Action restore) : IDisposable
  {
    private Action? restoreAction = restore;

    public void Dispose() => Interlocked.Exchange(ref restoreAction, null)?.Invoke();
  }
}
