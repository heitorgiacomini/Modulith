using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Data.MultiTenancy;
using Shared.DDD;

namespace Shared.Data.Filtering;

public static class DataFilterServiceCollectionExtensions
{
  public static IServiceCollection AddDataFilters(this IServiceCollection services)
  {
    services.AddOptions<DataFilterOptions>()
      .Configure(options =>
      {
        options.DefaultStates[typeof(ISoftDelete)] = true;
        options.DefaultStates[typeof(IMultiTenant)] = true;
      });
    services.TryAddSingleton<IDataFilter, DataFilter>();
    services.TryAddSingleton(typeof(IDataFilter<>), typeof(DataFilter<>));
    services.TryAddSingleton<ICurrentTenant, CurrentTenant>();
    return services;
  }
}
