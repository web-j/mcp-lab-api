namespace McpLab.Context;

/// <summary>Horário de Brasília para semanas do resumo e datas do cartão. Fuso por usuário fica para depois (ADR 0010).</summary>
internal static class BrazilClock
{
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var zone)
            ? zone
            : TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");

    public static DateTime ToLocal(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone).DateTime;

    /// <summary>Meia-noite do dia em Brasília, como instante absoluto (para filtros por data).</summary>
    public static DateTimeOffset StartOfDay(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
