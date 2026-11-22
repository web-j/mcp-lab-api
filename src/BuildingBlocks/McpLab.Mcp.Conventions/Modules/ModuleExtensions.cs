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

    /// <summary>Inicializa os módulos na ordem em que foram registrados (migrations, por exemplo).</summary>
    public static async Task InitializeModulesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        foreach (var module in scope.ServiceProvider.GetServices<IModule>())
        {
            await module.InitializeAsync(scope.ServiceProvider, cancellationToken);
        }
    }
}
