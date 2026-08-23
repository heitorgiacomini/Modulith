using Microsoft.Extensions.DependencyInjection;
using Shared.Data.Filtering;
using Shared.DDD;
using Xunit;

namespace SharedTests;

public sealed class DataFilterTests
{
  [Fact]
  public void Soft_delete_is_enabled_by_default()
  {
    using ServiceProvider provider = CreateProvider();
    IDataFilter dataFilter = provider.GetRequiredService<IDataFilter>();

    Assert.True(dataFilter.IsEnabled<ISoftDelete>());
  }

  [Fact]
  public void Nested_scopes_restore_the_exact_previous_state()
  {
    using ServiceProvider provider = CreateProvider();
    IDataFilter dataFilter = provider.GetRequiredService<IDataFilter>();

    using (dataFilter.Disable<ISoftDelete>())
    {
      Assert.False(dataFilter.IsEnabled<ISoftDelete>());
      using (dataFilter.Enable<ISoftDelete>())
      {
        Assert.True(dataFilter.IsEnabled<ISoftDelete>());
      }

      Assert.False(dataFilter.IsEnabled<ISoftDelete>());
    }

    Assert.True(dataFilter.IsEnabled<ISoftDelete>());
  }

  [Fact]
  public void Scope_restores_state_when_an_exception_is_thrown()
  {
    using ServiceProvider provider = CreateProvider();
    IDataFilter dataFilter = provider.GetRequiredService<IDataFilter>();

    Assert.Throws<InvalidOperationException>((Action)(() =>
    {
      using (dataFilter.Disable<ISoftDelete>())
      {
        Assert.False(dataFilter.IsEnabled<ISoftDelete>());
        throw new InvalidOperationException("Expected test exception");
      }
    }));

    Assert.True(dataFilter.IsEnabled<ISoftDelete>());
  }

  [Fact]
  public async Task Disabled_state_flows_across_await()
  {
    using ServiceProvider provider = CreateProvider();
    IDataFilter dataFilter = provider.GetRequiredService<IDataFilter>();

    using (dataFilter.Disable<ISoftDelete>())
    {
      await Task.Yield();
      Assert.False(dataFilter.IsEnabled<ISoftDelete>());
    }

    Assert.True(dataFilter.IsEnabled<ISoftDelete>());
  }

  [Fact]
  public async Task Concurrent_flows_do_not_change_each_others_state()
  {
    using ServiceProvider provider = CreateProvider();
    IDataFilter dataFilter = provider.GetRequiredService<IDataFilter>();
    TaskCompletionSource disabled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource checkedEnabled = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Task first = Task.Run(async () =>
    {
      using (dataFilter.Disable<ISoftDelete>())
      {
        disabled.SetResult();
        await checkedEnabled.Task;
        Assert.False(dataFilter.IsEnabled<ISoftDelete>());
      }
    });
    Task second = Task.Run(async () =>
    {
      await disabled.Task;
      Assert.True(dataFilter.IsEnabled<ISoftDelete>());
      checkedEnabled.SetResult();
    });

    await Task.WhenAll(first, second);
    Assert.True(dataFilter.IsEnabled<ISoftDelete>());
  }

  private static ServiceProvider CreateProvider()
  {
    ServiceCollection services = new();
    services.AddDataFilters();
    return services.BuildServiceProvider();
  }
}
