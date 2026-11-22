using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;

namespace McpLab.Host.Security;

/// <summary>Proteções de borda do servidor público (ADR 0008).</summary>
public static class EdgeProtectionExtensions
{
    public const string McpRateLimit = "mcp";

    public static WebApplicationBuilder AddEdgeProtection(this WebApplicationBuilder builder)
    {
        var edge = builder.Configuration.GetSection(EdgeOptions.SectionName).Get<EdgeOptions>() ?? new EdgeOptions();

        builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = edge.MaxRequestBodyBytes);

        // O container só é alcançável pelo nginx de borda (rede Docker), então os headers dele são confiáveis.
        builder.Services.Configure<ForwardedHeadersOptions>(o =>
        {
            o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            o.KnownIPNetworks.Clear();
            o.KnownProxies.Clear();
        });

        // Clientes MCP que rodam no navegador (MCP Inspector, apps web) precisam de CORS. A autenticação é por
        // bearer token, nunca por cookie, então liberar a origem não abre CSRF.
        builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "DELETE")
            .WithExposedHeaders("Mcp-Session-Id", "Mcp-Protocol-Version", "WWW-Authenticate")));

        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };

            o.AddPolicy(McpRateLimit, context =>
            {
                var userId = context.User.FindFirst("sub")?.Value;

                return userId is not null
                    ? RateLimitPartition.GetTokenBucketLimiter($"user:{userId}", _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = edge.UserRequestsPerMinute,
                        TokensPerPeriod = edge.UserRequestsPerMinute,
                        ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    })
                    : RateLimitPartition.GetFixedWindowLimiter($"ip:{context.Connection.RemoteIpAddress}", _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = edge.AnonymousRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    });
            });
        });

        return builder;
    }
}
