namespace Shared.Hosting.RateLimiting;

public sealed class ApplicationRateLimitingOptions
{
  public const string SectionName = "RateLimiting";

  public int PermitLimit { get; set; }

  public int WindowSeconds { get; set; }
}
