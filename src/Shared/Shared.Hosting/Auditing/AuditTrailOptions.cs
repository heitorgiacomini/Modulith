namespace Shared.Hosting.Auditing;

public sealed class AuditTrailOptions
{
  public const string SectionName = "AuditTrail";
  public const long MaximumLocalRetentionBytes = 1_073_741_824;

  public string DirectoryPath { get; set; } = "logs/audit";
  public string FileNamePrefix { get; set; } = "eshop-audit";
  public long MaxFileSizeBytes { get; set; } = 134_217_728;
  public int RetainedFileCount { get; set; } = 8;
}
