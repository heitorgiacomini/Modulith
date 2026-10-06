using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Data.Auditing;

namespace Shared.Hosting.Auditing;

public static class AuditTrailServiceCollectionExtensions
{
  public static IServiceCollection AddEshopAuditTrail(
    this IServiceCollection services,
    IConfiguration configuration)
  {
    _ = services
      .AddOptions<AuditTrailOptions>()
      .Bind(configuration.GetSection(AuditTrailOptions.SectionName))
      .Validate(options => !string.IsNullOrWhiteSpace(options.DirectoryPath),
        "AuditTrail:DirectoryPath is required.")
      .Validate(options => !string.IsNullOrWhiteSpace(options.FileNamePrefix),
        "AuditTrail:FileNamePrefix is required.")
      .Validate(options => options.MaxFileSizeBytes > 0,
        "AuditTrail:MaxFileSizeBytes must be positive.")
      .Validate(options => options.RetainedFileCount > 0,
        "AuditTrail:RetainedFileCount must be positive.")
      .Validate(options =>
          options.MaxFileSizeBytes <=
          AuditTrailOptions.MaximumLocalRetentionBytes / options.RetainedFileCount,
        "AuditTrail files must have a combined maximum size of 1 GiB or less.")
      .ValidateOnStart();

    services.TryAddSingleton<IAuditTrail, FileAuditTrail>();
    return services;
  }
}
