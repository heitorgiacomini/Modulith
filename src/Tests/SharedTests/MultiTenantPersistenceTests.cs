using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Data;
using Shared.Data.Filtering;
using Shared.Data.Interceptors;
using Shared.Data.MultiTenancy;
using Shared.DDD;
using Xunit;

namespace SharedTests;

public sealed class MultiTenantPersistenceTests
{
  private static readonly Guid Acme = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid Contoso = Guid.Parse("22222222-2222-2222-2222-222222222222");

  [Fact]
  public async Task Save_assigns_current_tenant_and_filters_other_tenants_and_soft_deleted_rows()
  {
    using ServiceProvider services = CreateServices();
    ICurrentTenant currentTenant = services.GetRequiredService<ICurrentTenant>();
    IDataFilter dataFilter = services.GetRequiredService<IDataFilter>();
    string databaseName = Guid.NewGuid().ToString();

    using (currentTenant.Change(Acme))
    {
      await using TenantDbContext context = CreateContext(databaseName, dataFilter, currentTenant);
      TenantEntity active = new() { Id = Guid.NewGuid(), Name = "Acme active" };
      context.TenantEntities.Add(active);
      await context.SaveChangesAsync();
      Assert.Equal(Acme, active.TenantId);
    }

    using (currentTenant.Change(Contoso))
    {
      await using TenantDbContext context = CreateContext(databaseName, dataFilter, currentTenant);
      context.TenantEntities.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Contoso active" });
      context.TenantEntities.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Contoso deleted", IsDeleted = true });
      await context.SaveChangesAsync();
      Assert.Single(await context.TenantEntities.ToListAsync());
    }

    using (currentTenant.Change(Acme))
    await using (TenantDbContext context = CreateContext(databaseName, dataFilter, currentTenant))
    {
      Assert.Equal("Acme active", (await context.TenantEntities.SingleAsync()).Name);
      using (dataFilter.Disable<IMultiTenant>())
      {
        Assert.Equal(2, await context.TenantEntities.CountAsync());
        using (dataFilter.Disable<ISoftDelete>())
        {
          Assert.Equal(3, await context.TenantEntities.CountAsync());
        }
      }
    }
  }

  [Fact]
  public async Task Save_rejects_missing_mismatched_and_changed_tenant_ownership()
  {
    using ServiceProvider services = CreateServices();
    ICurrentTenant currentTenant = services.GetRequiredService<ICurrentTenant>();
    IDataFilter dataFilter = services.GetRequiredService<IDataFilter>();
    string databaseName = Guid.NewGuid().ToString();

    await using (TenantDbContext hostContext = CreateContext(databaseName, dataFilter, currentTenant))
    {
      hostContext.TenantEntities.Add(new TenantEntity { Id = Guid.NewGuid() });
      await Assert.ThrowsAsync<InvalidOperationException>(() => hostContext.SaveChangesAsync());
    }

    using (currentTenant.Change(Acme))
    {
      await using TenantDbContext mismatchContext = CreateContext(databaseName, dataFilter, currentTenant);
      mismatchContext.TenantEntities.Add(new TenantEntity { Id = Guid.NewGuid(), TenantId = Contoso });
      await Assert.ThrowsAsync<InvalidOperationException>(() => mismatchContext.SaveChangesAsync());
    }

    Guid entityId;
    using (currentTenant.Change(Acme))
    {
      await using TenantDbContext createContext = CreateContext(databaseName, dataFilter, currentTenant);
      TenantEntity entity = new() { Id = Guid.NewGuid() };
      createContext.Add(entity);
      await createContext.SaveChangesAsync();
      entityId = entity.Id;
    }

    using (currentTenant.Change(Acme))
    await using (TenantDbContext updateContext = CreateContext(databaseName, dataFilter, currentTenant))
    {
      TenantEntity entity = await updateContext.TenantEntities.SingleAsync(value => value.Id == entityId);
      entity.TenantId = Contoso;
      await Assert.ThrowsAsync<InvalidOperationException>(() => updateContext.SaveChangesAsync());
    }
  }

  [Fact]
  public void Full_audited_interface_is_transitively_soft_delete_capable()
  {
    Assert.True(typeof(ISoftDelete).IsAssignableFrom(typeof(FullAuditedEntity<Guid>)));
    Assert.False(typeof(ISoftDelete).IsAssignableFrom(typeof(HostEntity)));
  }

  private static ServiceProvider CreateServices()
  {
    ServiceCollection services = new();
    services.AddDataFilters();
    return services.BuildServiceProvider();
  }

  private static TenantDbContext CreateContext(
    string databaseName,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant)
  {
    DbContextOptions<TenantDbContext> options = new DbContextOptionsBuilder<TenantDbContext>()
      .UseInMemoryDatabase(databaseName)
      .AddInterceptors(new MultiTenantEntityInterceptor(currentTenant))
      .Options;
    return new TenantDbContext(options, dataFilter, currentTenant);
  }

  private sealed class TenantDbContext(
    DbContextOptions<TenantDbContext> options,
    IDataFilter dataFilter,
    ICurrentTenant currentTenant) : DbContext(options), IDataFilterContext
  {
    public DbSet<TenantEntity> TenantEntities => Set<TenantEntity>();
    public DbSet<HostEntity> HostEntities => Set<HostEntity>();
    public bool IsSoftDeleteFilterEnabled => dataFilter.IsEnabled<ISoftDelete>();
    public bool IsMultiTenantFilterEnabled => dataFilter.IsEnabled<IMultiTenant>();
    public Guid? CurrentTenantId => currentTenant.Id;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TenantEntity>().HasKey(entity => entity.Id);
      modelBuilder.Entity<HostEntity>().HasKey(entity => entity.Id);
      modelBuilder.ApplyDataFilters(this);
    }
  }

  private sealed class TenantEntity : Entity<Guid>, IMultiTenant, ISoftDelete
  {
    public Guid? TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public string? Name { get; set; }
  }

  private sealed class HostEntity : Entity<Guid>;
}
