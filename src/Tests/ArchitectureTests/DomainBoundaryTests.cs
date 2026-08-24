using System.Reflection;
using Shared.DDD;
using Xunit;

namespace ArchitectureTests;

public sealed class DomainBoundaryTests
{
    private static readonly Assembly[] ModuleAssemblies =
    [
        typeof(Accounts.Accounts.Domain.CustomerAccount).Assembly,
        typeof(Basket.Basket.Domain.ShoppingCart).Assembly,
        typeof(Catalog.Products.Domain.Product).Assembly,
        typeof(Ordering.Orders.Domain.Order).Assembly
    ];

    [Fact]
    public void DomainTypes_DoNotExposeAspNetCoreDependencies()
    {
        Type[] domainTypes = ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(domainTypes);
        foreach (Type type in domainTypes)
        {
            IEnumerable<Type> dependencies = type.GetInterfaces()
                .Concat(type.GetProperties().Select(property => property.PropertyType))
                .Concat(type.GetMethods().Select(method => method.ReturnType))
                .Concat(type.GetMethods().SelectMany(method => method.GetParameters()).Select(parameter => parameter.ParameterType))
                .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType));

            Assert.DoesNotContain(dependencies, dependency =>
                dependency.Namespace?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);
        }
    }

    [Fact]
    public void DomainEntities_DoNotHavePublicConstructors()
    {
        Type[] entityTypes = ModuleAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true)
            .Where(type => typeof(IEntity).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(entityTypes);
        Assert.All(entityTypes, type => Assert.Empty(type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)));
    }
}
