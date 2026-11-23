using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpLab.IntegrationTests;

/// <summary>Busca e linha do tempo sob demanda (change contexto-portatil, entrega 3).</summary>
public sealed class ContextHistoryTests(PostgresHostFixture host) : IClassFixture<PostgresHostFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_ignores_accents_and_finds_actions_with_their_reference()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());
        await RecordAsync(client, 32m, "Farmácia");
        await RecordAsync(client, 80m, "Mercado");

        var page = await SearchAsync(client, new() { ["text"] = "farmacia" });

        Assert.Equal(1, page.Count);
        Assert.Contains("Despesa R$ 32,00 · Farmácia (app desconhecido) [testing:sample:", page.Results);
        Assert.DoesNotContain("Mercado", page.Results);
    }

    [Fact]
    public async Task Search_finds_a_word_fragment()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());
        await RecordAsync(client, 32m, "Farmácia");

        Assert.Equal(1, (await SearchAsync(client, new() { ["text"] = "farm" })).Count);
    }

    [Fact]
    public async Task Search_reaches_completed_notes_and_old_handoffs_that_left_the_card()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());
        var pending = await SaveAsync(client, "pending", "comprar remédio na farmácia");
        await client.CallToolAsync("context_update_note", new Dictionary<string, object?> { ["number"] = pending, ["action"] = "complete" }, cancellationToken: Ct);
        await SaveAsync(client, "handoff", "parei revisando gastos de farmácia");
        await SaveAsync(client, "handoff", "parei no orçamento de dezembro");

        var page = await SearchAsync(client, new() { ["text"] = "farmácia" });

        Assert.Equal(2, page.Count);
        Assert.Contains($"#{pending} pendência (concluída)", page.Results);
        Assert.Contains("parei revisando gastos de farmácia", page.Results);
    }

    [Fact]
    public async Task Kind_filter_returns_only_notes_of_that_kind()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());
        await RecordAsync(client, 10m, "Mercado");
        await SaveAsync(client, "decision", "mercado fica em R$ 1.200 por mês");
        await SaveAsync(client, "pending", "comparar preço do mercado");

        var page = await SearchAsync(client, new() { ["text"] = "mercado", ["kind"] = "decision" });

        Assert.Equal(1, page.Count);
        Assert.Contains("decisão", page.Results);
    }

    [Fact]
    public async Task Pages_of_at_most_twenty_with_cursor()
    {
        var user = Guid.NewGuid();
        await SeedEventsAsync(user, 25, "Despesa R$ 5,00 · Padaria");
        await using var client = await ConnectAsync(user);

        var first = await SearchAsync(client, new() { ["text"] = "padaria", ["limit"] = 50 });
        var second = await SearchAsync(client, new() { ["text"] = "padaria", ["limit"] = 50, ["cursor"] = first.NextCursor });

        Assert.Equal(20, first.Count);
        Assert.NotNull(first.NextCursor);
        Assert.Equal(5, second.Count);
        Assert.Null(second.NextCursor);
    }

    [Fact]
    public async Task Period_filter_uses_brazilian_dates()
    {
        var user = Guid.NewGuid();
        await host.QueryAsync(db => db.Database.ExecuteSqlAsync($"""
            INSERT INTO context.events (id, user_id, occurred_at, module, tool, client_id, summary) VALUES
              (gen_random_uuid(), {user}, '2026-10-01 12:00-03', 'testing', 'testing_record_sample', 'seed', 'Despesa outubro'),
              (gen_random_uuid(), {user}, '2026-11-02 00:30-03', 'testing', 'testing_record_sample', 'seed', 'Despesa novembro')
            """, Ct));
        await using var client = await ConnectAsync(user);

        var october = await ListAsync(client, new() { ["since"] = "2026-10-01", ["until"] = "2026-10-31" });
        var november = await SearchAsync(client, new() { ["text"] = "despesa", ["since"] = "2026-11-02" });

        Assert.Equal(1, october.Count);
        Assert.Contains("Despesa outubro", october.Results);
        Assert.Equal(1, november.Count);
        Assert.Contains("Despesa novembro", november.Results);
    }

    [Fact]
    public async Task Activity_list_is_newest_first_and_filters_by_module()
    {
        var user = Guid.NewGuid();
        await host.QueryAsync(db => db.Database.ExecuteSqlAsync($"""
            INSERT INTO context.events (id, user_id, occurred_at, module, tool, client_id, summary) VALUES
              (gen_random_uuid(), {user}, now() - interval '2 hours', 'finance', 'finance_x', 'seed', 'Despesa antiga'),
              (gen_random_uuid(), {user}, now() - interval '1 hour', 'finance', 'finance_x', 'seed', 'Despesa nova'),
              (gen_random_uuid(), {user}, now(), 'scheduling', 'scheduling_x', 'seed', 'Compromisso dentista')
            """, Ct));
        await using var client = await ConnectAsync(user);

        var finance = await ListAsync(client, new() { ["module"] = "finance" });

        Assert.Equal(2, finance.Count);
        Assert.True(finance.Results.IndexOf("Despesa nova", StringComparison.Ordinal) < finance.Results.IndexOf("Despesa antiga", StringComparison.Ordinal));
        Assert.DoesNotContain("dentista", finance.Results);
    }

    [Fact]
    public async Task Search_never_shows_another_users_history()
    {
        await using (var ana = await ConnectAsync(Guid.NewGuid()))
        {
            await RecordAsync(ana, 99m, "Joalheria");
        }

        await using var bruno = await ConnectAsync(Guid.NewGuid());
        var page = await SearchAsync(bruno, new() { ["text"] = "joalheria" });

        Assert.Equal(0, page.Count);
        Assert.Equal("Nada encontrado.", page.Results);
    }

    [Fact]
    public async Task Invalid_cursor_teaches_to_search_again()
    {
        await using var client = await ConnectAsync(Guid.NewGuid());

        var result = await client.CallToolAsync("context_list_activity", new Dictionary<string, object?> { ["cursor"] = "lixo" }, cancellationToken: Ct);

        Assert.True(result.IsError);
        Assert.Contains("sem cursor", Assert.IsType<TextContentBlock>(result.Content[0]).Text);
    }

    private sealed record Page(string Results, int Count, string? NextCursor);

    private static async Task<Page> SearchAsync(McpClient client, Dictionary<string, object?> args) =>
        ToPage(await client.CallToolAsync("context_search_history", args, cancellationToken: Ct));

    private static async Task<Page> ListAsync(McpClient client, Dictionary<string, object?> args) =>
        ToPage(await client.CallToolAsync("context_list_activity", args, cancellationToken: Ct));

    private static Page ToPage(CallToolResult result)
    {
        Assert.NotEqual(true, result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.StructuredContent));
        var cursor = json.TryGetProperty("nextCursor", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
        return new Page(json.GetProperty("results").GetString()!, json.GetProperty("count").GetInt32(), cursor);
    }

    private static Task RecordAsync(McpClient client, decimal amount, string category) =>
        client.CallToolAsync("testing_record_sample", new Dictionary<string, object?> { ["amount"] = amount, ["category"] = category }, cancellationToken: Ct).AsTask();

    private static async Task<int> SaveAsync(McpClient client, string kind, string text)
    {
        var result = await client.CallToolAsync("context_save_note", new Dictionary<string, object?> { ["kind"] = kind, ["text"] = text }, cancellationToken: Ct);
        return JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(result.StructuredContent)).GetProperty("number").GetInt32();
    }

    private Task SeedEventsAsync(Guid user, int count, string summary) => host.QueryAsync(db => db.Database.ExecuteSqlAsync($"""
        INSERT INTO context.events (id, user_id, occurred_at, module, tool, client_id, summary)
        SELECT gen_random_uuid(), {user}, now() - (n || ' minutes')::interval, 'testing', 'testing_record_sample', 'seed', {summary}
        FROM generate_series(1, {count}) AS n
        """, Ct));

    private Task<McpClient> ConnectAsync(Guid userId) =>
        host.Factory.ConnectMcpAsync(TestTokens.OAuthAccessToken(userId: userId), Ct);
}
