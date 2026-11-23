using System.Globalization;
using System.Text.Json;
using McpLab.Context.Contracts;
using McpLab.Context.Persistence;
using Microsoft.EntityFrameworkCore;

namespace McpLab.Context.Activity;

internal sealed record ActivityRecord(Guid UserId, string ClientId, string Module, string Tool, ActivityEntry Entry, DateTimeOffset OccurredAt);

/// <summary>Grava eventos na linha do tempo e atualiza o resumo semanal na mesma transação.</summary>
internal sealed class ActivityLog(ContextDbContext db)
{
    /// <returns><c>false</c> quando o evento já existia (mesma chave de idempotência).</returns>
    public async Task<bool> RecordAsync(ActivityRecord record, CancellationToken cancellationToken)
    {
        var entry = record.Entry;
        var metrics = entry.Metrics is { Count: > 0 } ? JsonSerializer.Serialize(entry.Metrics) : null;
        var isoWeek = IsoWeekOf(record.OccurredAt);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var inserted = await db.Database.SqlQuery<Guid>($"""
            INSERT INTO context.events (id, user_id, occurred_at, module, tool, client_id, summary, entity_ref, metrics, idempotency_key)
            VALUES ({Guid.CreateVersion7()}, {record.UserId}, {record.OccurredAt}, {record.Module}, {record.Tool}, {record.ClientId},
                    {entry.Summary}, {entry.EntityRef}, CAST({metrics} AS jsonb), {entry.IdempotencyKey})
            ON CONFLICT (user_id, tool, idempotency_key) WHERE idempotency_key IS NOT NULL DO NOTHING
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);

        if (inserted.Count == 0)
        {
            return false;
        }

        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO context.weekly_rollups (user_id, module, iso_week, event_count, metrics, updated_at)
            VALUES ({record.UserId}, {record.Module}, {isoWeek}, 1, CAST({metrics} AS jsonb), {record.OccurredAt})
            ON CONFLICT (user_id, module, iso_week) DO UPDATE SET
                event_count = weekly_rollups.event_count + 1,
                metrics = context.jsonb_sum(weekly_rollups.metrics, EXCLUDED.metrics),
                updated_at = EXCLUDED.updated_at
            """, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public static string IsoWeekOf(DateTimeOffset moment)
    {
        var local = BrazilClock.ToLocal(moment);
        return string.Create(CultureInfo.InvariantCulture, $"{ISOWeek.GetYear(local)}-W{ISOWeek.GetWeekOfYear(local):00}");
    }
}
