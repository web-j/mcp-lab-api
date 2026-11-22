using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace McpLab.Mcp.Conventions.Modules;

/// <summary>
/// Um módulo de negócio. Registra os próprios serviços e as próprias tools, resources e prompts;
/// o host não conhece nada além desta interface.
/// </summary>
public interface IModule
{
    /// <summary>Prefixo de todas as tools do módulo (<c>finance</c> em <c>finance_record_expense</c>).</summary>
    string Name { get; }

    /// <summary>Uma frase sobre o que o módulo atende. Aparece em <c>system_get_status</c>.</summary>
    string Description { get; }

    void Register(IHostApplicationBuilder builder, IMcpServerBuilder mcp);

    /// <summary>
    /// Roda uma vez na subida do host, antes de aceitar requisições. Módulos com banco aplicam as migrations aqui.
    /// </summary>
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;
}
