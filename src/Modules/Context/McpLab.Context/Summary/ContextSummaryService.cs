using McpLab.Context.Notes;
using McpLab.Context.Persistence;
using McpLab.Mcp.Conventions.Identity;
using Microsoft.EntityFrameworkCore;

namespace McpLab.Context.Summary;

/// <summary>Busca só o necessário para o cartão (consultas limitadas) e entrega ao <see cref="SummaryCardBuilder"/>.</summary>
internal sealed class ContextSummaryService(ContextDbContext db, ICurrentUser user, TimeProvider time)
{
    public const int BriefBudget = 1_500;
    public const int FullBudget = 4_000;

    public async Task<(string Card, int Omitted)> BuildAsync(bool full, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow();
        var userId = user.Id;
        var (noteLimit, eventLimit, weekLimit) = full ? (100, 20, 8) : (30, 8, 4);

        var handoff = await db.Notes.AsNoTracking()
            .Where(n => n.UserId == userId && n.Kind == "handoff" && n.Status == NoteKinds.Open)
            .OrderByDescending(n => n.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var openQuery = db.Notes.AsNoTracking()
            .Where(n => n.UserId == userId && n.Status == NoteKinds.Open && n.Kind != "handoff" && (n.Pinned || n.Kind == "pending" || n.Kind == "goal"));
        var openTotal = await openQuery.CountAsync(cancellationToken);
        var openNotes = await openQuery
            .OrderByDescending(n => n.Pinned).ThenByDescending(n => n.UpdatedAt)
            .Take(noteLimit)
            .ToListAsync(cancellationToken);

        var weekAgo = now.AddDays(-7);
        var events = await db.Events.AsNoTracking()
            .Where(e => e.UserId == userId && e.OccurredAt >= weekAgo)
            .OrderByDescending(e => e.OccurredAt)
            .Take(eventLimit)
            .ToListAsync(cancellationToken);

        var weeks = await db.WeeklyRollups.AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.IsoWeek).ThenBy(w => w.Module)
            .Take(weekLimit)
            .ToListAsync(cancellationToken);

        var clientIds = events.Select(e => e.ClientId).Append(handoff?.ClientId).OfType<string>().Distinct().ToList();
        var appNames = await db.Clients.AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId))
            .ToDictionaryAsync(c => c.ClientId, c => c.Name, cancellationToken);

        return new SummaryCardBuilder(now, appNames).Build(
            full ? FullBudget : BriefBudget, canExpand: !full, handoff, openNotes, openTotal, events, weeks);
    }
}
