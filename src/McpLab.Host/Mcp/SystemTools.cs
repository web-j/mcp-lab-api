using System.ComponentModel;
using McpLab.Mcp.Conventions.Modules;
using ModelContextProtocol.Server;

namespace McpLab.Host.Mcp;

[McpServerToolType]
public sealed class SystemTools(IEnumerable<IModule> modules, IHostEnvironment environment, TimeProvider time)
{
    [McpServerTool(
        Name = "system_get_status",
        Title = "Status do servidor",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("""
        Mostra se o servidor está no ar, a versão, o ambiente e quais módulos de negócio estão carregados.
        Use para verificar a conexão ou descobrir quais áreas (finanças, agendamento...) o servidor atende
        antes de procurar uma tool específica. Não altera nada.
        """)]
    public ServerStatus GetStatus() => new(
        Server: "mcp-lab-api",
        Version: ServerVersion.Current,
        Environment: environment.EnvironmentName,
        UtcNow: time.GetUtcNow(),
        Modules: [.. modules.Select(m => new ModuleInfo(m.Name, m.Description))]);
}

public sealed record ServerStatus(
    string Server,
    string Version,
    string Environment,
    DateTimeOffset UtcNow,
    IReadOnlyList<ModuleInfo> Modules);

public sealed record ModuleInfo(
    [property: Description("Prefixo das tools do módulo, ex.: finance")] string Name,
    string Description);
