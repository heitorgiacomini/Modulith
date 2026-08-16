using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using System.Reflection;
using Xunit;
using ReflectionAssembly = System.Reflection.Assembly;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public sealed class ModuleDependencyTests
{
    private static readonly ReflectionAssembly[] ModuleAssemblies = Directory
        .EnumerateFiles(AppContext.BaseDirectory, "*.dll")
        .Select(TryLoadAssembly)
        .OfType<ReflectionAssembly>()
        .Where(IsModuleAssembly)
        .ToArray();

    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(ModuleAssemblies)
        .Build();

    [Fact]
    public void Module_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        Assert.True(ModuleAssemblies.Length > 1, "At least two module assemblies must be discovered.");

        var modules = ModuleAssemblies
            .Select(assembly => Types().That().ResideInAssembly(assembly).As($"{assembly.GetName().Name} module"))
            .ToArray();

        for (var moduleIndex = 0; moduleIndex < modules.Length; moduleIndex++)
        {
            for (var otherModuleIndex = 0; otherModuleIndex < modules.Length; otherModuleIndex++)
            {
                if (moduleIndex == otherModuleIndex)
                {
                    continue;
                }

                Types().That().Are(modules[moduleIndex])
                    .Should().NotDependOnAny(modules[otherModuleIndex])
                    .Check(Architecture);
            }
        }
    }

    private static ReflectionAssembly? TryLoadAssembly(string assemblyPath)
    {
        try
        {
            return ReflectionAssembly.LoadFrom(assemblyPath);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    private static bool IsModuleAssembly(ReflectionAssembly assembly)
    {
        var markerTypeName = $"{assembly.GetName().Name}Module";

        try
        {
            return assembly.GetExportedTypes().Any(type =>
                type.IsAbstract && type.IsSealed && type.Name == markerTypeName);
        }
        catch (ReflectionTypeLoadException)
        {
            return false;
        }
    }
}
