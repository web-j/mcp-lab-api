using McpLab.Context.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace McpLab.IntegrationTests;

/// <summary>
/// Host completo com Postgres de verdade (Testcontainers) e as migrations aplicadas na subida, mais tools de teste
/// (<see cref="TestingTools"/>) para exercitar o registro automático da linha do tempo.
/// </summary>
public sealed class PostgresHostFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseTestAuthentication()
            .UseSetting("ConnectionStrings:mcplab", _postgres.GetConnectionString())
            .ConfigureTestServices(services => services.AddMcpServer().WithTools<TestingTools>()));

        // Força a subida do host (e as migrations) antes dos testes.
        _ = Factory.Server;
    }

    internal async Task<T> QueryAsync<T>(Func<ContextDbContext, Task<T>> query)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        return await query(scope.ServiceProvider.GetRequiredService<ContextDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
