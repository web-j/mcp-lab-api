using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpLab.Mcp.Conventions.Modules;

public static class ModuleExtensions
{
    public static IHostApplicationBuilder AddModules(this IHostApplicationBuilder builder, IMcpServerBuilder mcp, params IModule[] modules)
    {
        foreach (var module in modules)
        {
            if (!ToolNaming.IsValidPrefix(module.Name))
            {
                throw new InvalidOperationException($"Nome de módulo inválido: '{module.Name}'. Use só letras minúsculas, ex.: finance.");
            }

            module.Register(builder, mcp);
            builder.Services.AddSingleton(module);
        }

        return builder;
    }
}
