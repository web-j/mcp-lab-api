namespace McpLab.Host.Security;

public sealed class EdgeOptions
{
    public const string SectionName = "Edge";

    /// <summary>Chamadas por minuto de cada usuário autenticado.</summary>
    public int UserRequestsPerMinute { get; init; } = 120;

    /// <summary>Chamadas por minuto de cada IP sem token (descoberta OAuth, tentativas inválidas).</summary>
    public int AnonymousRequestsPerMinute { get; init; } = 30;

    public long MaxRequestBodyBytes { get; init; } = 1_048_576;
}
