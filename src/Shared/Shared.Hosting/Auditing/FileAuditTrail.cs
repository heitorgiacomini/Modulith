using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Data.Auditing;
using Shared.Hosting.Observability;

namespace Shared.Hosting.Auditing;

public sealed class FileAuditTrail : IAuditTrail, IDisposable
{
  private static readonly string[] SensitiveNameFragments =
  [
    "password", "secret", "token", "authorization", "card", "payment", "expiration", "last4",
    "email", "phone", "address", "postal", "body", "graphql", "query", "document",
    "credential", "apikey", "cookie"
  ];

  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
  };

  private readonly object sync = new();
  private readonly AuditTrailOptions options;
  private readonly ILogger<FileAuditTrail> logger;
  private readonly string directoryPath;
  private StreamWriter? writer;
  private string? currentFilePath;
  private long currentFileLength;
  private int sequence;
  private bool disposed;

  public FileAuditTrail(
    IOptions<AuditTrailOptions> options,
    IHostEnvironment hostEnvironment,
    ILogger<FileAuditTrail> logger)
  {
    this.options = options.Value;
    this.logger = logger;
    directoryPath = Path.IsPathRooted(this.options.DirectoryPath)
      ? this.options.DirectoryPath
      : Path.Combine(hostEnvironment.ContentRootPath, this.options.DirectoryPath);
  }

  public void Write(AuditEventV1 auditEvent)
  {
    ArgumentNullException.ThrowIfNull(auditEvent);

    try
    {
      AuditEventV1 safeAuditEvent = RedactSensitiveValues(auditEvent);
      string json = JsonSerializer.Serialize(safeAuditEvent, SerializerOptions);
      int encodedLength = Encoding.UTF8.GetByteCount(json) + Environment.NewLine.Length;

      lock (sync)
      {
        ObjectDisposedException.ThrowIf(disposed, this);
        EnsureWriter(encodedLength);
        writer!.WriteLine(json);
        writer.Flush();
        currentFileLength += encodedLength;
      }

      EshopTelemetry.AuditEventsWritten.Add(
        1,
        new KeyValuePair<string, object?>("audit.outcome", auditEvent.Outcome.ToString()));
    }
    catch (Exception exception)
    {
      EshopTelemetry.AuditWriteFailures.Add(1);
      logger.LogWarning(
        exception,
        "Best-effort audit event {AuditEventId} could not be written.",
        auditEvent.EventId);
    }
  }

  private static AuditEventV1 RedactSensitiveValues(AuditEventV1 auditEvent) => auditEvent with
  {
    Changes = auditEvent.Changes.ToDictionary(
      change => change.Key,
      change => IsSensitiveName(change.Key)
        ? new AuditValueChangeV1("[REDACTED]", "[REDACTED]")
        : change.Value),
    Metadata = RedactMetadata(auditEvent.Metadata)
  };

  private static IReadOnlyDictionary<string, object?> RedactMetadata(
    IReadOnlyDictionary<string, object?> metadata) =>
    metadata.ToDictionary(
      item => item.Key,
      item => IsSensitiveName(item.Key)
        ? "[REDACTED]"
        : item.Value switch
        {
          IReadOnlyDictionary<string, object?> nested => RedactMetadata(nested),
          IDictionary<string, object?> nested => RedactMetadata(
            new Dictionary<string, object?>(nested)),
          _ => item.Value
        });

  private static bool IsSensitiveName(string name) =>
    SensitiveNameFragments.Any(fragment =>
      name.Contains(fragment, StringComparison.OrdinalIgnoreCase));

  public void Dispose()
  {
    lock (sync)
    {
      if (disposed)
      {
        return;
      }

      disposed = true;
      writer?.Dispose();
      writer = null;
    }
  }

  private void EnsureWriter(int encodedLength)
  {
    if (writer is null)
    {
      OpenNewFile();
      return;
    }

    if (currentFileLength > 0 &&
      currentFileLength + encodedLength > options.MaxFileSizeBytes)
    {
      writer.Dispose();
      writer = null;
      OpenNewFile();
    }
  }

  private void OpenNewFile()
  {
    Directory.CreateDirectory(directoryPath);
    string timestamp = DateTimeOffset.UtcNow.ToString(
      "yyyyMMdd'T'HHmmssfff'Z'",
      System.Globalization.CultureInfo.InvariantCulture);
    currentFilePath = Path.Combine(
      directoryPath,
      $"{options.FileNamePrefix}-{timestamp}-{Environment.ProcessId}-{sequence++:D4}.ndjson");

    FileStream stream = new(
      currentFilePath,
      FileMode.Append,
      FileAccess.Write,
      FileShare.ReadWrite | FileShare.Delete);
    writer = new StreamWriter(stream, new UTF8Encoding(false))
    {
      AutoFlush = true
    };
    currentFileLength = stream.Length;
    DeleteExpiredFiles();
  }

  private void DeleteExpiredFiles()
  {
    IEnumerable<FileInfo> expiredFiles = new DirectoryInfo(directoryPath)
      .EnumerateFiles($"{options.FileNamePrefix}-*.ndjson", SearchOption.TopDirectoryOnly)
      .Where(file => !string.Equals(file.FullName, currentFilePath, StringComparison.OrdinalIgnoreCase))
      .OrderByDescending(file => file.LastWriteTimeUtc)
      .Skip(options.RetainedFileCount - 1);

    foreach (FileInfo expiredFile in expiredFiles)
    {
      try
      {
        expiredFile.Delete();
      }
      catch (IOException exception)
      {
        logger.LogWarning(
          exception,
          "Expired audit file {AuditFilePath} could not be deleted.",
          expiredFile.FullName);
      }
      catch (UnauthorizedAccessException exception)
      {
        logger.LogWarning(
          exception,
          "Expired audit file {AuditFilePath} could not be deleted.",
          expiredFile.FullName);
      }
    }
  }
}
