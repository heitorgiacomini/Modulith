using Carter;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Shared.Extensions;

public static class CarterExtensions
{
	public static IServiceCollection AddCarterWithAssemblies
		(this IServiceCollection services, params Assembly[] assemblies)
	{

		_ = services.AddCarter(configurator: config =>
		{
			foreach (Assembly assembly in assemblies)
			{
				Type[] modules = assembly.GetTypes()
					.Where(t => t.IsAssignableTo(typeof(ICarterModule)) && !t.IsAbstract && !t.IsInterface).ToArray();
				//.Where(t => t.IsAssignableTo(typeof(ICarterModule)) )).ToArray();

				_ = config.WithModules(modules);
			}
		});


		return services;
	}
}

//webAppbuilder.Services.AddCarter(configurator: config =>
//{
//	Type[] catalogModules = typeof(CatalogModule).Assembly.GetTypes()
//		.Where(t => t.IsAssignableTo(typeof(ICarterModule))).ToArray();

//_ = config.WithModules(catalogModules);

//});