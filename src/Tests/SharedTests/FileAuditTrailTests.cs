using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shared.Data.Auditing;
using Shared.Hosting.Auditing;
using Xunit;

namespace SharedTests;

public sealed class FileAuditTrailTests
{
  [Theory]
  [InlineData(AuditOutcome.Succeeded)]
  [InlineData(AuditOutcome.Failed)]
  [InlineData(AuditOutcome.Denied)]
  public void Writes_each_explicit_outcome_as_one_top_level_json_object_per_line(
    AuditOutcome outcome)
  {
    using TemporaryDirectory temporaryDirectory = new();
    AuditTrailOptions options = new()
    {
      DirectoryPath = "audit",
      FileNamePrefix = "test-audit",
      MaxFileSizeBytes = 1024 * 1024,
      RetainedFileCount = 2,
    };
    using FileAuditTrail auditTrail = CreateAuditTrail(temporaryDirectory.Path, options);
    AuditEventV1 auditEvent = CreateEvent(outcome);

    auditTrail.Write(auditEvent);
    auditTrail.Dispose();

    string file = Assert.Single(Directory.GetFiles(
      Path.Combine(temporaryDirectory.Path, "audit"),
      "*.ndjson"));
    string line = Assert.Single(File.ReadLines(file));
    using JsonDocument document = JsonDocument.Parse(line);
    Assert.Equal(auditEvent.EventId, document.RootElement.GetProperty("eventId").GetGuid());
    Assert.Equal(outcome.ToString(), document.RootElement.GetProperty("outcome").GetString());
    Assert.Equal("test-trace", document.RootElement.GetProperty("traceId").GetString());
  }

  [Fact]
  public void Redacts_sensitive_explicit_changes_and_metadata_by_key()
  {
    using TemporaryDirectory temporaryDirectory = new();
    AuditTrailOptions options = new()
    {
      DirectoryPath = "audit",
      FileNamePrefix = "test-audit",
      MaxFileSizeBytes = 1024 * 1024,
      RetainedFileCount = 2,
    };
    using FileAuditTrail auditTrail = CreateAuditTrail(temporaryDirectory.Path, options);
    AuditEventV1 auditEvent = CreateEvent(AuditOutcome.Denied) with
    {
      Changes = new Dictionary<string, AuditValueChangeV1>
      {
        ["GraphQLDocument"] = new("old-secret", "new-secret")
      },
      Metadata = new Dictionary<string, object?>
      {
        ["authorizationHeader"] = "Bearer secret-token",
        ["safe"] = true
      }
    };

    auditTrail.Write(auditEvent);
    auditTrail.Dispose();

    string file = Assert.Single(Directory.GetFiles(
      Path.Combine(temporaryDirectory.Path, "audit"),
      "*.ndjson"));
    string json = Assert.Single(File.ReadLines(file));
    Assert.DoesNotContain("old-secret", json);
    Assert.DoesNotContain("new-secret", json);
    Assert.DoesNotContain("Bearer secret-token", json);
    Assert.Contains("[REDACTED]", json);
  }

  [Fact]
  public void Rotation_enforces_the_bounded_retained_file_count()
  {
    using TemporaryDirectory temporaryDirectory = new();
    AuditTrailOptions options = new()
    {
      DirectoryPath = "audit",
      FileNamePrefix = "test-audit",
      MaxFileSizeBytes = 300,
      RetainedFileCount = 2,
    };
    using FileAuditTrail auditTrail = CreateAuditTrail(temporaryDirectory.Path, options);

    for (int index = 0; index < 10; index++)
    {
      auditTrail.Write(CreateEvent(AuditOutcome.Succeeded) with
      {
        Metadata = new Dictionary<string, object?>
        {
          ["padding"] = new string('x', 200),
        },
      });
    }

    string[] files = Directory.GetFiles(
      Path.Combine(temporaryDirectory.Path, "audit"),
      "*.ndjson");
    Assert.InRange(files.Length, 1, options.RetainedFileCount);
  }

  private static FileAuditTrail CreateAuditTrail(string contentRoot, AuditTrailOptions options) =>
    new(
      Options.Create(options),
      new TestHostEnvironment(contentRoot),
      NullLogger<FileAuditTrail>.Instance);

  private static AuditEventV1 CreateEvent(AuditOutcome outcome) => new()
  {
    EventId = Guid.NewGuid(),
    OccurredAtUtc = DateTimeOffset.UtcNow,
    Service = "test-service",
    Module = "Test",
    Category = AuditCategory.Security,
    Action = "AccessResource",
    Outcome = outcome,
    Actor = new AuditActorV1(Guid.NewGuid()),
    Subject = new AuditSubjectV1("Resource", "resource-1"),
    TraceId = "test-trace",
    SpanId = "test-span",
    CorrelationId = "test-correlation",
  };

  private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
  {
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "SharedTests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }

  private sealed class TemporaryDirectory : IDisposable
  {
    public TemporaryDirectory()
    {
      Path = System.IO.Path.Combine(
        System.IO.Path.GetTempPath(),
        "eshop-audit-tests",
        Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose() => Directory.Delete(Path, recursive: true);
  }
}
