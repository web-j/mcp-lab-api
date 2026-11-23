using System.Globalization;

namespace McpLab.Context.Summary;

/// <summary>Formatos curtos de data e app usados no cartão e nos resultados de busca (mesmo jeito de ler).</summary>
internal sealed class ContextText(DateTimeOffset now, IReadOnlyDictionary<string, string?> appNames)
{
    public string App(string clientId) =>
        appNames.TryGetValue(clientId, out var name) && !string.IsNullOrWhiteSpace(name) ? name : "app desconhecido";

    public string Ago(DateTimeOffset moment)
    {
        var elapsed = now - moment;
        return elapsed.TotalMinutes < 60 ? $"há {Math.Max(1, (int)elapsed.TotalMinutes)} min"
            : elapsed.TotalHours < 24 ? $"há {(int)elapsed.TotalHours} h"
            : $"em {BrazilClock.ToLocal(moment).ToString("dd/MM", CultureInfo.InvariantCulture)}";
    }

    /// <summary><c>hoje 14:02</c>, <c>ontem 09:10</c>, <c>03/10 18:00</c> ou <c>03/10/2025 18:00</c> em outro ano.</summary>
    public string Day(DateTimeOffset moment)
    {
        var local = BrazilClock.ToLocal(moment);
        var today = BrazilClock.ToLocal(now).Date;
        var day = local.Date == today ? "hoje"
            : local.Date == today.AddDays(-1) ? "ontem"
            : local.Year == today.Year ? local.ToString("dd/MM", CultureInfo.InvariantCulture)
            : local.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return $"{day} {local.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }
}
