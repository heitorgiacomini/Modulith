using Shared.Data.Filtering;

namespace Shared.Data;

// Used only to build EF migration models, never to authorize runtime access.
public sealed class DesignTimeDataFilter : IDataFilter
{
  public bool IsEnabled<TFilter>() where TFilter : class => true;
  public IDisposable Enable<TFilter>() where TFilter : class => throw new NotSupportedException();
  public IDisposable Disable<TFilter>() where TFilter : class => throw new NotSupportedException();
}
