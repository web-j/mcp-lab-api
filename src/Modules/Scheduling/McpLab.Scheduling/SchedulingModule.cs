using McpLab.Mcp.Conventions.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpLab.Scheduling;

public sealed class SchedulingModule : IModule
{
    public string Name => "scheduling";

    public string Description => "Agenda, disponibilidade e compromissos.";

    public void Register(IHostApplicationBuilder builder, IMcpServerBuilder mcp)
    {
        var assembly = typeof(SchedulingModule).Assembly;

        mcp.WithToolsFromAssembly(assembly)
           .WithResourcesFromAssembly(assembly)
           .WithPromptsFromAssembly(assembly);
    }
}
