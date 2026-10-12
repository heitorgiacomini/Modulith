using System.Collections.Concurrent;
using System.Reflection;
using Accounts.Data;
using Api.Infrastructure;
using Basket.Data;
using Catalog.Data;
using Catalog.Products.Domain;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Npgsql;
using Ordering.Data;
using Ordering.Orders.Dtos;
using Ordering.Orders.Features.CreateOrder;
using Shared.Data;
using Shared.Data.Auditing;
using Shared.Data.Filtering;
using Shared.Data.Interceptors;
using Shared.Data.MultiTenancy;
using Shared.Messaging.Events;
using Shared.Messaging.Outbox;
using Shared.Hosting.Auditing;
using Xunit;

namespace SharedTests;

public sealed class PostgresFactAttribute : FactAttribute
{
  public PostgresFactAttribute()
  {
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION")))
      Skip = "Set POSTGRES_TEST_CONNECTION to run PostgreSQL reliability tests (enabled in CI).";
  }
}

public sealed class PostgresReliabilityTests : IAsyncLifetime
{
  private readonly Guid tenantId = Guid.NewGuid();
  private readonly string databaseName = "modulith_tests_" + Guid.NewGuid().ToString("N");
  private ServiceProvider? services;
  private string? adminConnection;
  private readonly List<string> roles = [];

  public async Task InitializeAsync()
  {
    adminConnection = Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION");
    if (string.IsNullOrWhiteSpace(adminConnection)) return;
    await using var admin = new NpgsqlConnection(adminConnection);
    await admin.OpenAsync();
    await using (var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin))
      await create.ExecuteNonQueryAsync();
    var connection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName };
    var registrations = new ServiceCollection();
    registrations.AddLogging();
    registrations.AddScoped<ICurrentTenant, CurrentTenant>();
    registrations.AddScoped<ICurrentUser, BackgroundUser>();
    registrations.AddSingleton<IDataFilter, DesignTimeDataFilter>();
    registrations.AddMediatR(options => options.RegisterServicesFromAssemblies(typeof(Catalog.CatalogModule).Assembly, typeof(Ordering.OrderingModule).Assembly));
    AddContext<CatalogDbContext>(registrations, connection.ConnectionString);
    AddContext<BasketDbContext>(registrations, connection.ConnectionString);
    AddContext<OrderingDbContext>(registrations, connection.ConnectionString);
    AddContext<AccountsDbContext>(registrations, connection.ConnectionString);
    services = registrations.BuildServiceProvider();
    using var scope = services.CreateScope();
    foreach (DbContext context in new DbContext[] {
      scope.ServiceProvider.GetRequiredService<CatalogDbContext>(),
      scope.ServiceProvider.GetRequiredService<BasketDbContext>(),
      scope.ServiceProvider.GetRequiredService<OrderingDbContext>(),
      scope.ServiceProvider.GetRequiredService<AccountsDbContext>() })
    {
      await context.Database.MigrateAsync();
      Assert.False(context.Database.HasPendingModelChanges());
    }
  }

  private static void AddContext<TContext>(IServiceCollection registrations, string connection) where TContext : DbContext =>
    registrations.AddDbContext<TContext>((provider, options) => options.UseNpgsql(connection).AddInterceptors(
      new MultiTenantEntityInterceptor(provider.GetRequiredService<ICurrentTenant>()),
      new DispatchDomainEventsInterceptor(provider.GetRequiredService<IMediator>()),
      new AuditableEntityInterceptor(provider.GetRequiredService<ICurrentUser>(), provider.GetRequiredService<ICurrentTenant>())));

  [PostgresFact]
  public async Task Rollback_discards_business_changes_audit_records_and_price_events()
  {
    using var scope = services!.CreateScope();
    using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var product = Product.Create(Guid.NewGuid(), "Test", ["test"], "Test", "test.png", 10m);
    db.Products.Add(product);
    await db.SaveChangesAsync();
    int committedAudits = await db.Set<AuditRecord>().CountAsync();
    await using (var transaction = await db.Database.BeginTransactionAsync())
    {
      product.Update("Test", ["test"], "Test", "test.png", 20m);
      await db.SaveChangesAsync();
      Assert.Single(await db.Set<IntegrationOutboxMessage>().ToListAsync());
      await transaction.RollbackAsync();
    }
    db.ChangeTracker.Clear();
    Assert.Equal(10m, (await db.Products.SingleAsync()).Price);
    Assert.Equal(committedAudits, await db.Set<AuditRecord>().CountAsync());
    Assert.Empty(await db.Set<IntegrationOutboxMessage>().ToListAsync());
  }

  [PostgresFact]
  public async Task Broker_failure_keeps_message_for_retry_with_stable_identity()
  {
    Guid eventId = await StageMessageAsync();
    IBus bus = DispatchProxy.Create<IBus, RecordingBus>();
    var recording = (RecordingBus)(object)bus;
    recording.FailAfterAccepting = true;
    var dispatcher = Dispatcher(bus);
    await dispatcher.DispatchBatchAsync(default);
    using (var scope = services!.CreateScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
      var row = await db.Set<IntegrationOutboxMessage>().SingleAsync();
      Assert.Null(row.ProcessedOn);
      Assert.Equal(1, row.AttemptCount);
      row.NextAttemptAt = DateTime.UtcNow.AddSeconds(-1);
      await db.SaveChangesAsync();
    }
    recording.FailAfterAccepting = false;
    await dispatcher.DispatchBatchAsync(default);
    Assert.Equal(new[] { eventId, eventId }, recording.Accepted.ToArray());
    using var verify = services!.CreateScope();
    Assert.NotNull((await verify.ServiceProvider.GetRequiredService<CatalogDbContext>().Set<IntegrationOutboxMessage>().SingleAsync()).ProcessedOn);
  }

  [PostgresFact]
  public async Task Two_dispatchers_do_not_claim_the_same_pending_rows()
  {
    for (int index = 0; index < 12; index++) await StageMessageAsync();
    IBus bus = DispatchProxy.Create<IBus, RecordingBus>();
    var recording = (RecordingBus)(object)bus;
    await Task.WhenAll(Dispatcher(bus).DispatchBatchAsync(default), Dispatcher(bus).DispatchBatchAsync(default));
    Assert.Equal(12, recording.Accepted.Count);
    Assert.Equal(12, recording.Accepted.Distinct().Count());
  }

  [PostgresFact]
  public async Task Concurrent_checkout_replay_creates_one_order_and_one_receipt()
  {
    Guid eventId = Guid.NewGuid();
    AddressDto address = new("Test", "User", "user@example.test", "555", "Street", null, "City", "SP", "01000", "BR");
    OrderDto order = new(Guid.Empty, Guid.NewGuid(), "checkout", address, address,
      new("tok_test", "Test User", "visa", "4242", "12/30"), [new(Guid.Empty, Guid.NewGuid(), 1, 10m)]);
    async Task<Guid> Deliver()
    {
      using var scope = services!.CreateScope();
      var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
      using var change = tenant.Change(tenantId);
      var handler = new CreateOrderFromCheckoutHandler(scope.ServiceProvider.GetRequiredService<OrderingDbContext>(), tenant);
      return (await handler.Handle(new(order, eventId), default)).Id;
    }
    Guid[] ids = await Task.WhenAll(Deliver(), Deliver(), Deliver());
    Assert.Single(ids.Distinct());
    using var verify = services!.CreateScope();
    using var tenantScope = verify.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);
    var db = verify.ServiceProvider.GetRequiredService<OrderingDbContext>();
    Assert.Single(await db.Orders.ToListAsync());
    Assert.Single(await db.Set<CheckoutReceipt>().ToListAsync());
    Assert.NotEmpty(await db.Set<AuditRecord>().ToListAsync());
  }

  [PostgresFact]
  public async Task Readiness_verifies_the_migrated_module_database()
  {
    using var scope = services!.CreateScope();
    var check = new DatabaseReadinessCheck<CatalogDbContext>(scope.ServiceProvider.GetRequiredService<CatalogDbContext>());
    Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext())).Status);
  }

  [PostgresFact]
  public async Task Runtime_roles_cannot_modify_audit_history_or_read_another_module()
  {
    using var scope = services!.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    string prefix = "test_" + Guid.NewGuid().ToString("N") + "_";
    foreach (string module in new[] { "catalog", "basket", "ordering", "accounts" })
    {
      string role = prefix + module + "_app";
      await db.Database.ExecuteSqlRawAsync($"CREATE ROLE {role} LOGIN PASSWORD 'ci-only'");
      roles.Add(role);
    }
    DirectoryInfo? root = new(AppContext.BaseDirectory);
    while (root is not null && !File.Exists(Path.Combine(root.FullName, "src", "docker-config", "postgres", "runtime-permissions.sql")))
      root = root.Parent;
    string permissions = await File.ReadAllTextAsync(Path.Combine(root!.FullName, "src", "docker-config", "postgres", "runtime-permissions.sql"));
    permissions = permissions.Replace("module_name || '_app'", $"'{prefix}' || module_name || '_app'")
      .Replace("TO ordering_app", $"TO {prefix}ordering_app").Replace("FROM ordering_app", $"FROM {prefix}ordering_app");
    await db.Database.ExecuteSqlRawAsync(permissions);
    var runtimeConnection = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString())
    {
      Username = prefix + "catalog_app", Password = "ci-only"
    };
    await using var runtime = new NpgsqlConnection(runtimeConnection.ConnectionString);
    await runtime.OpenAsync();
    await using (var read = new NpgsqlCommand("SELECT count(*) FROM catalog.\"AuditRecords\"", runtime))
      await read.ExecuteScalarAsync();
    foreach (string sql in new[] {
      "UPDATE catalog.\"AuditRecords\" SET \"Payload\" = '{}'",
      "DELETE FROM catalog.\"AuditRecords\"",
      "TRUNCATE catalog.\"AuditRecords\"",
      "SELECT count(*) FROM basket.\"ShoppingCarts\"" })
    {
      await using var forbidden = new NpgsqlCommand(sql, runtime);
      var error = await Assert.ThrowsAsync<PostgresException>(() => forbidden.ExecuteNonQueryAsync());
      Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
  }

  [PostgresFact]
  public async Task Failed_file_export_retains_checkpoint_and_can_be_retried()
  {
    using (var scope = services!.CreateScope())
    {
      using var tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId);
      var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
      db.Products.Add(Product.Create(Guid.NewGuid(), "Audit", ["test"], "Test", "test.png", 10m));
      await db.SaveChangesAsync();
    }
    string path = Path.Combine(Path.GetTempPath(), "modulith_audit_" + Guid.NewGuid().ToString("N"));
    await File.WriteAllTextAsync(path, "Blocks directory creation");
    using var files = new FileAuditTrail(Options.Create(new AuditTrailOptions { DirectoryPath = path }),
      new AuditHost(), NullLogger<FileAuditTrail>.Instance);
    var worker = new AuditExportWorker<CatalogDbContext>(services!.GetRequiredService<IServiceScopeFactory>(), files,
      NullLogger<AuditExportWorker<CatalogDbContext>>.Instance);
    try
    {
      await Assert.ThrowsAsync<IOException>(() => worker.ExportBatchAsync(default));
      using (var scope = services!.CreateScope())
        Assert.Null((await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Set<AuditDelivery>().SingleAsync()).ExportedAtUtc);
      File.Delete(path);
      Assert.Equal(1, await worker.ExportBatchAsync(default));
      using var verify = services!.CreateScope();
      Assert.NotNull((await verify.ServiceProvider.GetRequiredService<CatalogDbContext>().Set<AuditDelivery>().SingleAsync()).ExportedAtUtc);
      Assert.Single(Directory.EnumerateFiles(path, "*.ndjson"));
    }
    finally
    {
      files.Dispose();
      if (File.Exists(path)) File.Delete(path);
      if (Directory.Exists(path))
      {
        foreach (string file in Directory.EnumerateFiles(path)) File.Delete(file);
        Directory.Delete(path);
      }
    }
  }

  private async Task<Guid> StageMessageAsync()
  {
    using var scope = services!.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var row = IntegrationOutboxMessage.From(new ProductPriceChangedIntegrationEvent { TenantId = tenantId, Price = 12m });
    db.Add(row);
    await db.SaveChangesAsync();
    return row.Id;
  }

  private IntegrationOutboxDispatcher<CatalogDbContext> Dispatcher(IBus bus) =>
    new(services!.GetRequiredService<IServiceScopeFactory>(), bus, NullLogger<IntegrationOutboxDispatcher<CatalogDbContext>>.Instance);

  public async Task DisposeAsync()
  {
    if (services is null || adminConnection is null) return;
    await services.DisposeAsync();
    NpgsqlConnection.ClearAllPools();
    await using var admin = new NpgsqlConnection(adminConnection);
    await admin.OpenAsync();
    // Only the unique database created by this fixture is removed.
    await using var drop = new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", admin);
    await drop.ExecuteNonQueryAsync();
    foreach (string role in roles)
    {
      await using var dropRole = new NpgsqlCommand($"DROP ROLE {role}", admin);
      await dropRole.ExecuteNonQueryAsync();
    }
  }

  private sealed class BackgroundUser : ICurrentUser
  {
    public Guid? Id => null;
    public string? UserName => null;
    public bool IsAuthenticated => false;
    public string? TraceId => null;
  }

  private sealed class AuditHost : IHostEnvironment
  {
    public string EnvironmentName { get; set; } = "Testing";
    public string ApplicationName { get; set; } = "ReliabilityTests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }

  public class RecordingBus : DispatchProxy
  {
    public ConcurrentQueue<Guid> Accepted { get; } = new();
    public bool FailAfterAccepting { get; set; }
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
      if (targetMethod?.Name != "Publish") throw new NotSupportedException(targetMethod?.Name);
      Accepted.Enqueue(((IntegrationEvent)args![0]!).EventId);
      return FailAfterAccepting ? Task.FromException(new IOException("Simulated broker outage after acceptance")) : Task.Delay(30);
    }
  }
}
