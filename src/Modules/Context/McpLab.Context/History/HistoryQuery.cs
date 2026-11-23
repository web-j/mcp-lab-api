using System.Text;
using McpLab.Context.Notes;
using McpLab.Context.Persistence;
using McpLab.Context.Summary;
using McpLab.Mcp.Conventions.Identity;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;

namespace McpLab.Context.History;

/// <summary>Uma linha de resultado vinda do SQL (nomes das colunas = nomes das propriedades).</summary>
internal sealed class HistoryRow
{
    public required string Source { get; init; }
    public Guid Id { get; init; }
    public DateTimeOffset At { get; init; }
    public string? Module { get; init; }
    public required string Text { get; init; }
    public string? Kind { get; init; }
    public int? Number { get; init; }
    public string? Status { get; init; }
    public required string ClientId { get; init; }
    public string? EntityRef { get; init; }
}

internal sealed record HistoryFilter(string? Module, string? Kind, DateOnly? Since, DateOnly? Until, string? Cursor, int Limit);

internal sealed record HistoryPage(string Results, int Count, string? NextCursor);

/// <summary>
/// Busca e listagem sob demanda (camada 2 do contexto): páginas de até 20 linhas curtas, do mais recente para o mais
/// antigo, com cursor. A busca usa o índice de texto em português e, para trechos curtos, procura também por substring.
/// </summary>
internal sealed class HistoryQuery(ContextDbContext db, ICurrentUser user, TimeProvider time)
{
    public const int MaxPageSize = 20;

    public async Task<HistoryPage> SearchAsync(string text, HistoryFilter filter, CancellationToken cancellationToken)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            throw new McpException("Informe o texto da busca em text. Ex.: farmácia.");
        }

        var like = "%" + text.ToLowerInvariant().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
        var (since, until, cursorAt, cursorId, limit) = Bounds(filter);

        var rows = await db.Database.SqlQuery<HistoryRow>($"""
            SELECT * FROM (
                SELECT 'action' AS "Source", e.id AS "Id", e.occurred_at AS "At", e.module AS "Module", e.summary AS "Text",
                       NULL::text AS "Kind", NULL::int AS "Number", NULL::text AS "Status", e.client_id AS "ClientId",
                       e.entity_ref AS "EntityRef"
                FROM context.events e
                WHERE e.user_id = {user.Id}
                  AND CAST({filter.Kind} AS text) IS NULL
                  AND (CAST({filter.Module} AS text) IS NULL OR e.module = CAST({filter.Module} AS text))
                  AND (CAST({since} AS timestamptz) IS NULL OR e.occurred_at >= CAST({since} AS timestamptz))
                  AND (CAST({until} AS timestamptz) IS NULL OR e.occurred_at < CAST({until} AS timestamptz))
                  AND (e.search @@ websearch_to_tsquery('portuguese', context.f_unaccent({text}))
                       OR context.f_unaccent(lower(e.summary)) LIKE context.f_unaccent({like}))
                UNION ALL
                SELECT 'note', n.id, n.updated_at, n.module, n.text, n.kind, n.number, n.status, n.client_id, NULL
                FROM context.notes n
                WHERE n.user_id = {user.Id}
                  AND (CAST({filter.Kind} AS text) IS NULL OR n.kind = CAST({filter.Kind} AS text))
                  AND (CAST({filter.Module} AS text) IS NULL OR n.module = CAST({filter.Module} AS text))
                  AND (CAST({since} AS timestamptz) IS NULL OR n.updated_at >= CAST({since} AS timestamptz))
                  AND (CAST({until} AS timestamptz) IS NULL OR n.updated_at < CAST({until} AS timestamptz))
                  AND (n.search @@ websearch_to_tsquery('portuguese', context.f_unaccent({text}))
                       OR context.f_unaccent(lower(n.text)) LIKE context.f_unaccent({like}))
            ) AS r
            WHERE CAST({cursorAt} AS timestamptz) IS NULL OR (r."At", r."Id") < (CAST({cursorAt} AS timestamptz), CAST({cursorId} AS uuid))
            ORDER BY r."At" DESC, r."Id" DESC
            LIMIT {limit + 1}
            """).ToListAsync(cancellationToken);

        return await PageAsync(rows, limit, cancellationToken);
    }

    public async Task<HistoryPage> ListActivityAsync(HistoryFilter filter, CancellationToken cancellationToken)
    {
        var (since, until, cursorAt, cursorId, limit) = Bounds(filter);

        var rows = await db.Database.SqlQuery<HistoryRow>($"""
            SELECT 'action' AS "Source", e.id AS "Id", e.occurred_at AS "At", e.module AS "Module", e.summary AS "Text",
                   NULL::text AS "Kind", NULL::int AS "Number", NULL::text AS "Status", e.client_id AS "ClientId",
                   e.entity_ref AS "EntityRef"
            FROM context.events e
            WHERE e.user_id = {user.Id}
              AND (CAST({filter.Module} AS text) IS NULL OR e.module = CAST({filter.Module} AS text))
              AND (CAST({since} AS timestamptz) IS NULL OR e.occurred_at >= CAST({since} AS timestamptz))
              AND (CAST({until} AS timestamptz) IS NULL OR e.occurred_at < CAST({until} AS timestamptz))
              AND (CAST({cursorAt} AS timestamptz) IS NULL OR (e.occurred_at, e.id) < (CAST({cursorAt} AS timestamptz), CAST({cursorId} AS uuid)))
            ORDER BY e.occurred_at DESC, e.id DESC
            LIMIT {limit + 1}
            """).ToListAsync(cancellationToken);

        return await PageAsync(rows, limit, cancellationToken);
    }

    private async Task<HistoryPage> PageAsync(List<HistoryRow> rows, int limit, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return new HistoryPage("Nada encontrado.", 0, null);
        }

        var page = rows.Take(limit).ToList();
        var next = rows.Count > limit ? HistoryCursor.Encode(page[^1].At, page[^1].Id) : null;

        var clientIds = page.Select(r => r.ClientId).Distinct().ToList();
        var appNames = await db.Clients.AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId))
            .ToDictionaryAsync(c => c.ClientId, c => c.Name, cancellationToken);
        var text = new ContextText(time.GetUtcNow(), appNames);

        var lines = new StringBuilder();
        foreach (var row in page)
        {
            if (lines.Length > 0)
            {
                lines.Append('\n');
            }

            lines.Append(row.Source == "action"
                ? $"Ação {text.Day(row.At)}: {row.Text} ({text.App(row.ClientId)})" + (row.EntityRef is null ? "" : $" [{row.EntityRef}]")
                : $"#{row.Number} {NoteKinds.Label(row.Kind!)}{(row.Status == NoteKinds.Done ? " (concluída)" : "")} {text.Day(row.At)}: {row.Text}");
        }

        return new HistoryPage(lines.ToString(), page.Count, next);
    }

    private static (DateTimeOffset? Since, DateTimeOffset? Until, DateTimeOffset? CursorAt, Guid? CursorId, int Limit) Bounds(HistoryFilter filter)
    {
        var limit = Math.Clamp(filter.Limit, 1, MaxPageSize);
        var since = filter.Since is { } s ? BrazilClock.StartOfDay(s) : (DateTimeOffset?)null;
        var until = filter.Until is { } u ? BrazilClock.StartOfDay(u.AddDays(1)) : (DateTimeOffset?)null;
        var (cursorAt, cursorId) = filter.Cursor is null ? (null, null) : HistoryCursor.Decode(filter.Cursor);
        return (since, until, cursorAt, cursorId, limit);
    }
}

/// <summary>Cursor opaco de paginação: data e ID do último item da página.</summary>
internal static class HistoryCursor
{
    public static string Encode(DateTimeOffset at, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{at.UtcTicks}:{id:N}")).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static (DateTimeOffset? At, Guid? Id) Decode(string cursor)
    {
        try
        {
            var padded = cursor.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(padded)).Split(':');
            return (new DateTimeOffset(long.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), TimeSpan.Zero), Guid.ParseExact(parts[1], "N"));
        }
        catch (Exception ex) when (ex is FormatException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            throw new McpException("Cursor inválido. Refaça a busca sem cursor.");
        }
    }
}
