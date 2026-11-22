namespace McpLab.Host.Auth;

public sealed class McpAuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Emissor dos tokens: o Auth do projeto Supabase do lab (<c>https://&lt;ref&gt;.supabase.co/auth/v1</c>).</summary>
    public required string Issuer { get; init; }

    /// <summary>O Supabase emite todo token com <c>aud = authenticated</c>; ele não vincula o token ao recurso (ADR 0007).</summary>
    public string Audience { get; init; } = "authenticated";

    /// <summary>URL pública do endpoint MCP, anunciada no Protected Resource Metadata.</summary>
    public required Uri Resource { get; init; }

    /// <summary>Página com login, cadastro e instruções de conexão.</summary>
    public Uri? Documentation { get; init; }
}
