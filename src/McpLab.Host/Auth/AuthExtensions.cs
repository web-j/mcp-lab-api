using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using McpLab.Mcp.Conventions.Identity;

namespace McpLab.Host.Auth;

public static class AuthExtensions
{
    public const string McpPolicy = "mcp";

    /// <summary>Claim que só tokens emitidos pelo servidor OAuth têm. Uma sessão comum do Supabase não acessa o MCP.</summary>
    public const string ClientIdClaim = "client_id";

    /// <summary>
    /// OAuth 2.1 com o Supabase como authorization server (ADR 0007). O host é só resource server:
    /// valida o JWT pelo JWKS do Supabase e publica o Protected Resource Metadata (RFC 9728) para os clientes
    /// descobrirem onde fazer login.
    /// </summary>
    public static WebApplicationBuilder AddMcpAuthentication(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetRequiredSection(McpAuthOptions.SectionName).Get<McpAuthOptions>()
            ?? throw new InvalidOperationException("Seção Auth ausente.");

        builder.Services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.Authority = options.Issuer;
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                    NameClaimType = "email",
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            })
            .AddMcp(o => o.ResourceMetadata = new()
            {
                Resource = options.Resource.ToString(),
                ResourceName = "Agateo MCP Lab",
                ResourceDocumentation = options.Documentation?.ToString(),
                AuthorizationServers = { options.Issuer },
                // offline_access: o cliente recebe refresh token e não desconecta a pessoa quando o token de 1 h expira.
                ScopesSupported = ["openid", "email", "offline_access"],
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(McpPolicy, p => p
                .RequireAuthenticatedUser()
                .RequireClaim("role", "authenticated")
                .RequireClaim(ClientIdClaim));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return builder;
    }
}
