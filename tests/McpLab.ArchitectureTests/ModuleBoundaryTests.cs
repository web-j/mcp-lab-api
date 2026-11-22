using System.Reflection;
using McpLab.Finance;
using McpLab.Mcp.Conventions.Modules;
using McpLab.Scheduling;

namespace McpLab.ArchitectureTests;

/// <summary>
/// Fronteiras do monolito modular (ADR 0001): um módulo só enxerga o outro pelo projeto
/// <c>*.Contracts</c>, e os building blocks não dependem de nenhum módulo.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly Assembly[] ModuleAssemblies = [typeof(FinanceModule).Assembly, typeof(SchedulingModule).Assembly];

    public static TheoryData<string> Modules() => [.. ModuleAssemblies.Select(a => a.GetName().Name!)];

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_references_other_modules_only_through_contracts(string moduleName)
    {
        var module = ModuleAssemblies.Single(a => a.GetName().Name == moduleName);
        var otherModules = ModuleAssemblies.Where(a => a != module).Select(a => a.GetName().Name).ToHashSet();

        var forbidden = module.GetReferencedAssemblies()
            .Select(r => r.Name)
            .Where(otherModules.Contains)
            .ToList();

        Assert.True(forbidden.Count == 0, $"{moduleName} referencia diretamente: {string.Join(", ", forbidden)}. Use o projeto .Contracts.");
    }

    [Fact]
    public void Building_blocks_do_not_depend_on_modules()
    {
        var buildingBlocks = new[] { typeof(IModule).Assembly, typeof(McpLab.SharedKernel.Error).Assembly };

        var forbidden = buildingBlocks
            .SelectMany(b => b.GetReferencedAssemblies().Select(r => (Block: b.GetName().Name, Reference: r.Name)))
            .Where(x => x.Reference?.StartsWith("McpLab.", StringComparison.Ordinal) == true)
            .Where(x => x.Reference is not ("McpLab.SharedKernel" or "McpLab.Mcp.Conventions"))
            .ToList();

        Assert.True(forbidden.Count == 0, string.Join(", ", forbidden));
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Every_module_exposes_exactly_one_IModule(string moduleName)
    {
        var module = ModuleAssemblies.Single(a => a.GetName().Name == moduleName);

        var implementations = module.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(t));

        Assert.Single(implementations);
    }
}
