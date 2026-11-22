using McpLab.Mcp.Conventions.Identity;

namespace McpLab.Host.Auth;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal User =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("Sem requisição HTTP ativa.");

    public Guid Id => Guid.Parse(User.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Token sem sub."));

    public string? Email => User.FindFirst("email")?.Value;

    public string ClientId => User.FindFirst(AuthExtensions.ClientIdClaim)?.Value
        ?? throw new InvalidOperationException("Token sem client_id.");
}
