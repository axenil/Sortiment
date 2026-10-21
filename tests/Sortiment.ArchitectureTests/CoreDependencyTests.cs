using System.Reflection;

namespace Sortiment.ArchitectureTests;

public sealed class CoreDependencyTests
{
    private static readonly Assembly CoreAssembly = Assembly.Load("Sortiment.Core");

    [Fact]
    public void CoreReferencesOnlyTheBaseClassLibrary()
    {
        var nonFrameworkReferences = CoreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => !IsBaseClassLibrary(name))
            .ToList();

        nonFrameworkReferences.ShouldBeEmpty();
    }

    // Microsoft.Extensions.*, EF Core and ASP.NET Core also start with "Microsoft.",
    // so only the exact BCL assembly names and the System.* namespace are allowed.
    private static bool IsBaseClassLibrary(string? assemblyName) =>
        assemblyName is "netstandard" or "mscorlib" or "System"
        || (assemblyName?.StartsWith("System.", StringComparison.Ordinal) ?? false);
}
