using McpLab.Mcp.Conventions.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpLab.Finance;

public sealed class FinanceModule : IModule
{
    public string Name => "finance";

    public string Description => "Contas, categorias e lançamentos financeiros (receitas e despesas).";

    public void Register(IHostApplicationBuilder builder, IMcpServerBuilder mcp)
    {
        var assembly = typeof(FinanceModule).Assembly;

        mcp.WithToolsFromAssembly(assembly)
           .WithResourcesFromAssembly(assembly)
           .WithPromptsFromAssembly(assembly);
    }
}
