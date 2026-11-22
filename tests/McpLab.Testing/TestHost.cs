using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ModelContextProtocol.Client;

namespace McpLab.Testing;

public static class TestHost
{
    /// <summary>Host em memória que aceita tokens de <see cref="TestTokens"/> no lugar dos do Supabase.</summary>
    public static IWebHostBuilder UseTestAuthentication(this IWebHostBuilder builder) => builder
        .UseEnvironment("Testing")
        .UseSetting("Auth:Issuer", TestTokens.Issuer)
        .UseSetting("Auth:Resource", "http://localhost/mcp")
        .ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
            configuration.SigningKeys.Add(TestTokens.SigningKey);
            o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        }));

    /// <summary>Cliente MCP real conectado em <c>/mcp</c> com o token informado.</summary>
    public static Task<McpClient> ConnectMcpAsync(this WebApplicationFactory<Program> factory, string accessToken, CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(factory.Server.BaseAddress, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {accessToken}" },
            },
            factory.CreateClient());

        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }
}
