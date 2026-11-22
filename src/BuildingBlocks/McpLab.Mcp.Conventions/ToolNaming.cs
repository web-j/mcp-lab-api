using System.Text.RegularExpressions;

namespace McpLab.Mcp.Conventions;

/// <summary>Convenção de nomes das tools: <c>modulo_verbo_objeto</c> em snake case (ADR 0005).</summary>
public static partial class ToolNaming
{
    /// <summary>Prefixo reservado para tools do próprio host, que não pertencem a nenhum módulo.</summary>
    public const string SystemPrefix = "system";

    public static bool IsValidName(string name) => NamePattern().IsMatch(name);

    public static bool IsValidPrefix(string prefix) => PrefixPattern().IsMatch(prefix);

    public static string PrefixOf(string name) => name.Split('_', 2)[0];

    [GeneratedRegex("^[a-z]+(_[a-z0-9]+){2,}$")]
    private static partial Regex NamePattern();

    [GeneratedRegex("^[a-z]+$")]
    private static partial Regex PrefixPattern();
}
