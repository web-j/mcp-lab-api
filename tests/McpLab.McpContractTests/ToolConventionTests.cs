using McpLab.Mcp.Conventions;
using ModelContextProtocol.Client;

namespace McpLab.McpContractTests;

/// <summary>
/// Garante as convenções de tools (docs/mcp-guidelines.md) para toda tool registrada,
/// inclusive as que forem criadas depois. Uma tool nova que fuja do padrão quebra o build.
/// </summary>
public sealed class ToolConventionTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private const int MinDescriptionLength = 60;

    [Fact]
    public async Task Server_exposes_at_least_the_system_tools()
    {
        var tools = await server.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains(tools, t => t.Name == "system_get_status");
    }

    [Fact]
    public async Task Every_tool_follows_the_conventions()
    {
        var tools = await server.Client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var prefixes = server.Modules.Select(m => m.Name).Append(ToolNaming.SystemPrefix).ToHashSet();

        var violations = tools.SelectMany(tool => Check(tool, prefixes)).ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static IEnumerable<string> Check(McpClientTool tool, IReadOnlySet<string> prefixes)
    {
        var protocol = tool.ProtocolTool;
        var annotations = protocol.Annotations;

        if (!ToolNaming.IsValidName(tool.Name))
            yield return $"{tool.Name}: nome fora do padrão modulo_verbo_objeto em snake case.";

        if (!prefixes.Contains(ToolNaming.PrefixOf(tool.Name)))
            yield return $"{tool.Name}: prefixo não corresponde a nenhum módulo registrado ({string.Join(", ", prefixes)}).";

        if (string.IsNullOrWhiteSpace(tool.Title))
            yield return $"{tool.Name}: falta Title.";

        if ((tool.Description?.Length ?? 0) < MinDescriptionLength)
            yield return $"{tool.Name}: descrição com menos de {MinDescriptionLength} caracteres; diga quando usar e quando não usar.";

        if (annotations?.ReadOnlyHint is null)
            yield return $"{tool.Name}: anotação ReadOnly não declarada.";

        if (annotations?.DestructiveHint is null)
            yield return $"{tool.Name}: anotação Destructive não declarada.";

        if (annotations?.IdempotentHint is null)
            yield return $"{tool.Name}: anotação Idempotent não declarada.";

        if (annotations?.OpenWorldHint is null)
            yield return $"{tool.Name}: anotação OpenWorld não declarada.";

        if (protocol.OutputSchema is null)
            yield return $"{tool.Name}: sem outputSchema; use UseStructuredContent = true.";
    }
}
