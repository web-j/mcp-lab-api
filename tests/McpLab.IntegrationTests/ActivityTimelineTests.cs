using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;

namespace McpLab.IntegrationTests;

/// <summary>Linha do tempo automática (change contexto-portatil, entrega 1).</summary>
public sealed class ActivityTimelineTests(PostgresHostFixture host) : IClassFixture<PostgresHostFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Successful_write_becomes_an_event_and_counts_in_the_week()
    {
        var user = Guid.NewGuid();
        await using var client = await ConnectAsync(user);

        var result = await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 80m }, cancellationToken: Ct);

        Assert.NotEqual(true, result.IsError);
        var events = await host.QueryAsync(db => db.Events.Where(e => e.UserId == user).ToListAsync(Ct));
        var single = Assert.Single(events);
        Assert.Equal("Despesa R$ 80,00 · Mercado", single.Summary);
        Assert.Equal("testing", single.Module);
        Assert.Equal("testing_record_sample", single.Tool);
        Assert.Equal(TestTokens.ClientId, single.ClientId);
        Assert.StartsWith("testing:sample:", single.EntityRef);

        var rollup = await host.QueryAsync(db => db.WeeklyRollups.SingleAsync(r => r.UserId == user, Ct));
        Assert.Equal(1, rollup.EventCount);
        Assert.Contains("\"amount\": 80", rollup.Metrics);
    }

    [Fact]
    public async Task Weekly_rollup_sums_metrics()
    {
        var user = Guid.NewGuid();
        await using var client = await ConnectAsync(user);

        await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 80m }, cancellationToken: Ct);
        await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 20.5m }, cancellationToken: Ct);

        var rollup = await host.QueryAsync(db => db.WeeklyRollups.SingleAsync(r => r.UserId == user, Ct));
        Assert.Equal(2, rollup.EventCount);
        Assert.Contains("\"amount\": 100.5", rollup.Metrics);
    }

    [Fact]
    public async Task Same_idempotency_key_records_one_event()
    {
        var user = Guid.NewGuid();
        await using var client = await ConnectAsync(user);
        var args = new Dictionary<string, object?> { ["amount"] = 80m, ["idempotencyKey"] = "lanc-1" };

        await client.CallToolAsync("testing_record_sample", args, cancellationToken: Ct);
        await client.CallToolAsync("testing_record_sample", args, cancellationToken: Ct);

        Assert.Equal(1, await host.QueryAsync(db => db.Events.CountAsync(e => e.UserId == user, Ct)));
        Assert.Equal(1, (await host.QueryAsync(db => db.WeeklyRollups.SingleAsync(r => r.UserId == user, Ct))).EventCount);
    }

    [Fact]
    public async Task Failed_write_records_nothing()
    {
        var user = Guid.NewGuid();
        await using var client = await ConnectAsync(user);

        var result = await client.CallToolAsync("testing_fail_sample", cancellationToken: Ct);

        Assert.True(result.IsError);
        Assert.Equal(0, await host.QueryAsync(db => db.Events.CountAsync(e => e.UserId == user, Ct)));
    }

    [Fact]
    public async Task Read_only_tool_records_nothing()
    {
        var user = Guid.NewGuid();
        await using var client = await ConnectAsync(user);

        await client.CallToolAsync("testing_read_sample", cancellationToken: Ct);

        Assert.Equal(0, await host.QueryAsync(db => db.Events.CountAsync(e => e.UserId == user, Ct)));
    }

    [Fact]
    public async Task Write_tool_that_forgets_to_record_fails_outside_production()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());

        var result = await client.CallToolAsync("testing_forget_sample", cancellationToken: Ct);

        Assert.True(result.IsError);
        Assert.Contains("IActivityRecorder", Assert.IsType<ModelContextProtocol.Protocol.TextContentBlock>(result.Content[0]).Text);
    }

    [Fact]
    public async Task Each_user_only_gets_own_events()
    {
        var (ana, bruno) = (Guid.NewGuid(), Guid.NewGuid());
        await using (var client = await ConnectAsync(ana))
        {
            await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 10m }, cancellationToken: Ct);
        }

        await using (var client = await ConnectAsync(bruno))
        {
            await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 20m }, cancellationToken: Ct);
        }

        var anaEvents = await host.QueryAsync(db => db.Events.Where(e => e.UserId == ana).Select(e => e.Summary).ToListAsync(Ct));
        Assert.Equal(["Despesa R$ 10,00 · Mercado"], anaEvents);
    }

    [Fact]
    public async Task Connected_app_is_remembered()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());

        await client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = 1m }, cancellationToken: Ct);

        // Sem chave secreta do Supabase nos testes, o app fica conhecido mas sem nome.
        var app = await host.QueryAsync(db => db.Clients.SingleAsync(c => c.ClientId == TestTokens.ClientId, Ct));
        Assert.Null(app.Name);
    }

    private Task<McpClient> ConnectAsync(Guid userId) =>
        host.Factory.ConnectMcpAsync(TestTokens.OAuthAccessToken(userId: userId), Ct);
}
