namespace McpLab.Context.Persistence;

/// <summary>Uma linha da linha do tempo: o que uma tool de escrita fez.</summary>
internal sealed class ActivityEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public required string Module { get; set; }
    public required string Tool { get; set; }
    public required string ClientId { get; set; }
    public required string Summary { get; set; }
    public string? EntityRef { get; set; }

    /// <summary>JSON com os números somados no resumo semanal.</summary>
    public string? Metrics { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>Nota escrita pela IA: objetivo, pendência, decisão, preferência, fato ou "onde paramos".</summary>
internal sealed class ContextNote
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Kind { get; set; }
    public required string Text { get; set; }
    public string? Module { get; set; }
    public required string Status { get; set; }
    public bool Pinned { get; set; }
    public required string ClientId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Resumo de uma semana ISO de um módulo, atualizado a cada evento.</summary>
internal sealed class WeeklyRollup
{
    public Guid UserId { get; set; }
    public required string Module { get; set; }

    /// <summary>Semana ISO no fuso de São Paulo, ex.: <c>2026-W47</c>.</summary>
    public required string IsoWeek { get; set; }
    public int EventCount { get; set; }
    public string? Metrics { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Cache do nome do app conectado (cliente OAuth registrado no Supabase).</summary>
internal sealed class ConnectedClient
{
    public required string ClientId { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
