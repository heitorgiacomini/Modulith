namespace Shared.Data.MultiTenancy;

public sealed class CurrentTenant : ICurrentTenant
{
  private readonly AsyncLocal<TenantState?> current = new();

  public Guid? Id => current.Value?.Id;
  public string? Name => current.Value?.Name;
  public bool IsAvailable => Id.HasValue;

  public IDisposable Change(Guid? id, string? name = null)
  {
    if (id == Guid.Empty)
    {
      throw new ArgumentException("A tenant identifier cannot be an empty GUID.", nameof(id));
    }

    TenantState? previous = current.Value;
    current.Value = id.HasValue ? new TenantState(id.Value, name) : null;
    return new RestoreTenant(() => current.Value = previous);
  }

  private sealed record TenantState(Guid Id, string? Name);

  private sealed class RestoreTenant(Action restore) : IDisposable
  {
    private Action? restoreAction = restore;

    public void Dispose() => Interlocked.Exchange(ref restoreAction, null)?.Invoke();
  }
}
