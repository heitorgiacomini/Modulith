using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Behaviors;
using System.Reflection;

namespace Shared.Extensions;

public static class MediatRExtension
{
    public static IServiceCollection AddMediatRWithAssemblies
          (this IServiceCollection services, params Assembly[] assemblies)
    {
        _ = services.AddMediatR(config =>
        {
            _ = config.RegisterServicesFromAssemblies(assemblies);
            _ = config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            _ = config.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        _ = services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }
}
