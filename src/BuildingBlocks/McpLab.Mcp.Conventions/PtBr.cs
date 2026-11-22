using System.Globalization;

namespace McpLab.Mcp.Conventions;

/// <summary>
/// Formatação dos textos que as tools devolvem e dos resumos da linha do tempo: sempre pt-BR, qualquer que seja a
/// cultura do servidor, e só com espaço comum (o separador do ICU muda entre versões e gasta token à toa).
/// </summary>
public static class PtBr
{
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary><c>80m</c> → <c>R$ 80,00</c>; <c>-1234.5m</c> → <c>-R$ 1.234,50</c>.</summary>
    public static string Money(decimal amount) =>
        (amount < 0 ? "-" : "") + "R$ " + Math.Abs(amount).ToString("N2", Culture);
}
