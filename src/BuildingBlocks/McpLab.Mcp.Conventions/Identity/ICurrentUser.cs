namespace McpLab.Mcp.Conventions.Identity;

/// <summary>
/// Quem está chamando a tool. Vem do token OAuth validado pelo host; os módulos usam
/// <see cref="Id"/> para filtrar todos os dados (cada usuário só enxerga os próprios).
/// </summary>
public interface ICurrentUser
{
    /// <summary>ID do usuário no Supabase (claim <c>sub</c>).</summary>
    Guid Id { get; }

    string? Email { get; }

    /// <summary>Cliente OAuth que fez a chamada (ChatGPT, Claude, Cursor...), registrado por DCR.</summary>
    string ClientId { get; }
}
