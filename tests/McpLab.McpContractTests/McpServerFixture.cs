using McpLab.Mcp.Conventions.Modules;
using McpLab.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace McpLab.McpContractTests;

/// <summary>
/// Sobe o host em memória, sem banco, e conecta um cliente MCP de verdade em <c>/mcp</c>, autenticado com um token
/// no formato do Supabase OAuth (assinado por <see cref="TestTokens.SigningKey"/>).
/// </summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory { get; } = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseTestAuthentication()
            // Só a string precisa existir: os testes de contrato não abrem conexão com o banco.
            .UseSetting("ConnectionStrings:mcplab", "Host=localhost;Database=mcplab;Username=test;Password=test")
            .UseSetting("Database:MigrateOnStartup", "false"));

    public McpClient Client { get; private set; } = null!;

    public IReadOnlyList<IModule> Modules => [.. Factory.Services.GetServices<IModule>()];

    public async ValueTask InitializeAsync() => Client = await ConnectAsync(TestTokens.OAuthAccessToken());

    public Task<McpClient> ConnectAsync(string accessToken) =>
        Factory.ConnectMcpAsync(accessToken, TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await Factory.DisposeAsync();
    }
}
