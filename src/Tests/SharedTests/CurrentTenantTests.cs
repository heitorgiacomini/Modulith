using Shared.Data.MultiTenancy;
using Xunit;

namespace SharedTests;

public sealed class CurrentTenantTests
{
  private static readonly Guid Acme = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid Contoso = Guid.Parse("22222222-2222-2222-2222-222222222222");

  [Fact]
  public void Default_state_is_host_context()
  {
    CurrentTenant tenant = new();

    Assert.Null(tenant.Id);
    Assert.Null(tenant.Name);
    Assert.False(tenant.IsAvailable);
  }

  [Fact]
  public void Nested_scopes_restore_the_previous_tenant()
  {
    CurrentTenant tenant = new();

    using (tenant.Change(Acme, "acme"))
    {
      Assert.Equal(Acme, tenant.Id);
      using (tenant.Change(Contoso, "contoso"))
      {
        Assert.Equal(Contoso, tenant.Id);
      }

      Assert.Equal(Acme, tenant.Id);
      Assert.Equal("acme", tenant.Name);
    }

    Assert.False(tenant.IsAvailable);
  }

  [Fact]
  public void Scope_restores_state_after_an_exception()
  {
    CurrentTenant tenant = new();

    Assert.Throws<InvalidOperationException>((Action)(() =>
    {
      using (tenant.Change(Acme))
      {
        throw new InvalidOperationException("Expected");
      }
    }));

    Assert.False(tenant.IsAvailable);
  }

  [Fact]
  public async Task State_flows_across_await()
  {
    CurrentTenant tenant = new();

    using (tenant.Change(Acme))
    {
      await Task.Yield();
      Assert.Equal(Acme, tenant.Id);
    }
  }

  [Fact]
  public async Task Concurrent_flows_are_isolated()
  {
    CurrentTenant tenant = new();
    TaskCompletionSource acmeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource contosoChecked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    Task first = Task.Run(async () =>
    {
      using (tenant.Change(Acme))
      {
        acmeEntered.SetResult();
        await contosoChecked.Task;
        Assert.Equal(Acme, tenant.Id);
      }
    });
    Task second = Task.Run(async () =>
    {
      await acmeEntered.Task;
      using (tenant.Change(Contoso))
      {
        Assert.Equal(Contoso, tenant.Id);
      }
      contosoChecked.SetResult();
    });

    await Task.WhenAll(first, second);
    Assert.False(tenant.IsAvailable);
  }

  [Fact]
  public void Empty_guid_is_rejected()
  {
    CurrentTenant tenant = new();

    Assert.Throws<ArgumentException>(() => tenant.Change(Guid.Empty));
  }
}
