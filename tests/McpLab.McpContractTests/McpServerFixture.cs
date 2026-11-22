using McpLab.Mcp.Conventions.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ModelContextProtocol.Client;

namespace McpLab.McpContractTests;

/// <summary>
/// Sobe o host em memória e conecta um cliente MCP de verdade em <c>/mcp</c>, autenticado com um token
/// no formato do Supabase OAuth (assinado por <see cref="TestTokens.SigningKey"/>).
/// </summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            // Só a string precisa existir: os testes de contrato não abrem conexão com o banco.
            .UseSetting("ConnectionStrings:mcplab", "Host=localhost;Database=mcplab;Username=test;Password=test")
            .UseSetting("Auth:Issuer", TestTokens.Issuer)
            .UseSetting("Auth:Resource", "http://localhost/mcp")
            .ConfigureTestServices(services => services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                var configuration = new OpenIdConnectConfiguration { Issuer = TestTokens.Issuer };
                configuration.SigningKeys.Add(TestTokens.SigningKey);
                o.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            })));

    public McpClient Client { get; private set; } = null!;

    public IReadOnlyList<IModule> Modules => [.. Factory.Services.GetServices<IModule>()];

    public async ValueTask InitializeAsync() => Client = await ConnectAsync(TestTokens.OAuthAccessToken());

    public Task<McpClient> ConnectAsync(string accessToken)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(Factory.Server.BaseAddress, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {accessToken}" },
            },
            Factory.CreateClient());

        return McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await Factory.DisposeAsync();
    }
}
