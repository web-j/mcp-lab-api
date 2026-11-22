namespace McpLab.Context.Contracts;

/// <summary>
/// O que uma tool de escrita fez, em uma linha curta montada por código (sem IA), para a linha do tempo do usuário.
/// Ex.: <c>new ActivityEntry("Despesa R$ 80,00 · Mercado", "finance:transaction:...", new() { ["amount"] = 80m })</c>.
/// </summary>
public sealed record ActivityEntry
{
    public const int MaxSummaryLength = 120;

    public ActivityEntry(
        string summary,
        string? entityRef = null,
        IReadOnlyDictionary<string, decimal>? metrics = null,
        string? idempotencyKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        if (summary.Length > MaxSummaryLength)
        {
            throw new ArgumentException($"O resumo tem {summary.Length} caracteres; o máximo é {MaxSummaryLength}.", nameof(summary));
        }

        Summary = summary;
        EntityRef = entityRef;
        Metrics = metrics;
        IdempotencyKey = idempotencyKey;
    }

    /// <summary>Uma linha, até 120 caracteres: o que aconteceu, com os valores que importam.</summary>
    public string Summary { get; }

    /// <summary>Referência à entidade afetada (<c>modulo:tipo:id</c>), para a IA buscar o detalhe na tool do módulo.</summary>
    public string? EntityRef { get; }

    /// <summary>Números somados no resumo semanal do módulo (ex.: <c>amount</c>).</summary>
    public IReadOnlyDictionary<string, decimal>? Metrics { get; }

    /// <summary>A mesma chave na mesma tool gera um único evento.</summary>
    public string? IdempotencyKey { get; }
}
