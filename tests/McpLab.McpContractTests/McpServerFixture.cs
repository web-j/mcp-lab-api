using McpLab.Mcp.Conventions.Modules;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;

namespace McpLab.McpContractTests;

/// <summary>Sobe o host em memória e conecta um cliente MCP de verdade em <c>/mcp</c>.</summary>
public sealed class McpServerFixture : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            // Só a string precisa existir: os testes de contrato não abrem conexão com o banco.
            .UseSetting("ConnectionStrings:mcplab", "Host=localhost;Database=mcplab;Username=test;Password=test"));

    public McpClient Client { get; private set; } = null!;

    public IReadOnlyList<IModule> Modules => [.. _factory.Services.GetServices<IModule>()];

    public async ValueTask InitializeAsync()
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(_factory.Server.BaseAddress, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            _factory.CreateClient());

        Client = await McpClient.CreateAsync(transport, cancellationToken: TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
