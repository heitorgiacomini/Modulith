using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Shared.Data;
using Shared.Data.Auditing;
using Shared.Data.Interceptors;
using Shared.DDD;
using Xunit;

namespace SharedTests;

public sealed class AuditableEntityInterceptorTests
{
  [Fact]
  public void Full_audited_entity_is_transitively_soft_deletable()
  {
    Assert.True(typeof(ISoftDelete).IsAssignableFrom(typeof(TestEntity)));
  }

  [Fact]
  public void Basic_and_outbox_entities_are_not_audited_or_soft_deletable()
  {
    Assert.False(typeof(ISoftDelete).IsAssignableFrom(typeof(BasicEntity)));
    Assert.False(typeof(IAuditedObject).IsAssignableFrom(typeof(Basket.Basket.Models.OutboxMessage)));
    Assert.False(typeof(ISoftDelete).IsAssignableFrom(typeof(Basket.Basket.Models.OutboxMessage)));
  }

  [Fact]
  public async Task Query_filter_is_applied_only_to_soft_deletable_entities()
  {
    await using TestDbContext dbContext = CreateContext(null, new ListLogger());

    Assert.NotEmpty(dbContext.Model.FindEntityType(typeof(TestEntity))!.GetDeclaredQueryFilters());
    Assert.Empty(dbContext.Model.FindEntityType(typeof(BasicEntity))!.GetDeclaredQueryFilters());
  }

  [Fact]
  public async Task Save_sets_authenticated_user_audit_metadata()
  {
    Guid userId = Guid.NewGuid();
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(userId, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Original", Secret = "hidden" };

    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();

    Assert.Equal(userId, entity.CreatedBy);
    Assert.Null(entity.LastModifiedBy);
    Assert.Null(entity.LastModified);
    Assert.NotEqual(default, entity.CreatedAt);
    Assert.Equal(TimeSpan.Zero, entity.CreatedAt.Offset);
    Assert.Contains(logger.Messages, message => message.Contains("Created", StringComparison.Ordinal));
    Assert.DoesNotContain(logger.Messages, message => message.Contains("hidden", StringComparison.Ordinal));
    Assert.Contains(logger.Messages, message => message.Contains("[REDACTED]", StringComparison.Ordinal));
  }

  [Fact]
  public async Task Update_preserves_creation_metadata_and_sets_modifier()
  {
    Guid creatorId = Guid.NewGuid();
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(creatorId, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Original", Secret = "hidden" };
    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();
    DateTimeOffset createdAt = entity.CreatedAt;

    Guid modifierId = Guid.NewGuid();
    dbContext.CurrentUser.Id = modifierId;
    entity.Name = "Updated";
    await dbContext.SaveChangesAsync();

    Assert.Equal(createdAt, entity.CreatedAt);
    Assert.Equal(creatorId, entity.CreatedBy);
    Assert.Equal(modifierId, entity.LastModifiedBy);
    Assert.NotNull(entity.LastModified);
  }

  [Fact]
  public async Task Remove_soft_deletes_and_default_query_filter_hides_entity()
  {
    Guid userId = Guid.NewGuid();
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(userId, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Original", Secret = "hidden" };
    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();

    dbContext.Entities.Remove(entity);
    await dbContext.SaveChangesAsync();

    Assert.True(entity.IsDeleted is true);
    Assert.Equal(userId, entity.DeletedBy);
    Assert.NotNull(entity.DeletedAt);
    Assert.Empty(await dbContext.Entities.ToListAsync());
    Assert.Single(await dbContext.Entities.IgnoreQueryFilters().ToListAsync());
    Assert.Contains(logger.Messages, message => message.Contains("Deleted", StringComparison.Ordinal));
  }

  [Fact]
  public async Task Background_save_uses_null_actor()
  {
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(null, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Background", Secret = "hidden" };

    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();

    Assert.Null(entity.CreatedBy);
    Assert.Null(entity.LastModifiedBy);
  }

  [Fact]
  public async Task Repeated_delete_preserves_original_deletion_metadata()
  {
    Guid originalDeleter = Guid.NewGuid();
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(originalDeleter, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Original", Secret = "hidden" };
    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();
    dbContext.Entities.Remove(entity);
    await dbContext.SaveChangesAsync();
    DateTimeOffset? deletedAt = entity.DeletedAt;

    dbContext.CurrentUser.Id = Guid.NewGuid();
    dbContext.Entities.Remove(entity);
    await dbContext.SaveChangesAsync();

    Assert.Equal(originalDeleter, entity.DeletedBy);
    Assert.Equal(deletedAt, entity.DeletedAt);
  }

  [Fact]
  public async Task Owned_entity_change_updates_aggregate_metadata()
  {
    Guid modifier = Guid.NewGuid();
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(modifier, logger);
    TestEntity entity = new() { Id = Guid.NewGuid(), Name = "Original", Secret = "hidden" };
    dbContext.Entities.Add(entity);
    await dbContext.SaveChangesAsync();

    entity.Detail.Value = "Changed";
    await dbContext.SaveChangesAsync();

    Assert.Equal(modifier, entity.LastModifiedBy);
    Assert.NotNull(entity.LastModified);
  }

  [Fact]
  public async Task Failed_save_does_not_emit_successful_audit_event()
  {
    ListLogger logger = new();
    TestCurrentUser currentUser = new() { Id = Guid.NewGuid() };
    AuditableEntityInterceptor auditInterceptor = new(currentUser, logger);
    DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .AddInterceptors(auditInterceptor, new FailingSaveInterceptor())
      .Options;
    await using TestDbContext dbContext = new(options, currentUser);
    dbContext.Entities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Failure", Secret = "hidden" });

    await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync());

    Assert.Empty(logger.Messages);
  }

  [Fact]
  public async Task Basic_entity_is_physically_deleted_without_audit_event()
  {
    ListLogger logger = new();
    await using TestDbContext dbContext = CreateContext(Guid.NewGuid(), logger);
    BasicEntity entity = new() { Id = Guid.NewGuid(), Name = "Basic" };
    dbContext.BasicEntities.Add(entity);
    await dbContext.SaveChangesAsync();
    logger.Messages.Clear();

    dbContext.BasicEntities.Remove(entity);
    await dbContext.SaveChangesAsync();

    Assert.Empty(await dbContext.BasicEntities.ToListAsync());
    Assert.Empty(logger.Messages);
  }

  private static TestDbContext CreateContext(Guid? userId, ListLogger logger)
  {
    TestCurrentUser currentUser = new() { Id = userId };
    AuditableEntityInterceptor interceptor = new(currentUser, logger);
    DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
      .UseInMemoryDatabase(Guid.NewGuid().ToString())
      .AddInterceptors(interceptor)
      .Options;
    return new TestDbContext(options, currentUser);
  }

  private sealed class TestDbContext(DbContextOptions<TestDbContext> options, TestCurrentUser currentUser)
    : DbContext(options)
  {
    public TestCurrentUser CurrentUser { get; } = currentUser;
    public DbSet<TestEntity> Entities => Set<TestEntity>();
    public DbSet<BasicEntity> BasicEntities => Set<BasicEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<TestEntity>().HasKey(entity => entity.Id);
      modelBuilder.Entity<TestEntity>().OwnsOne(entity => entity.Detail);
      modelBuilder.Entity<BasicEntity>().HasKey(entity => entity.Id);
      modelBuilder.ApplySoftDeleteQueryFilters();
    }
  }

  private sealed class TestEntity : FullAuditedEntity<Guid>
  {
    public string Name { get; set; } = string.Empty;
    [AuditSensitive] public string Secret { get; set; } = string.Empty;
    public OwnedDetail Detail { get; set; } = new();
  }

  private sealed class BasicEntity : Entity<Guid>
  {
    public string Name { get; set; } = string.Empty;
  }

  private sealed class OwnedDetail
  {
    public string Value { get; set; } = "Initial";
  }

  private sealed class TestCurrentUser : ICurrentUser
  {
    public Guid? Id { get; set; }
    public string? UserName => null;
    public bool IsAuthenticated => Id is not null;
    public string? TraceId => "test-trace";
  }

  private sealed class ListLogger : ILogger<AuditableEntityInterceptor>
  {
    public List<string> Messages { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
  }

  private sealed class FailingSaveInterceptor : SaveChangesInterceptor
  {
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
      DbContextEventData eventData,
      InterceptionResult<int> result,
      CancellationToken cancellationToken = default) =>
      ValueTask.FromException<InterceptionResult<int>>(new InvalidOperationException("Simulated failure"));
  }
}
