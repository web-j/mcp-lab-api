namespace McpLab.Context;

/// <summary>Horário de Brasília para semanas do resumo e datas do cartão. Fuso por usuário fica para depois (ADR 0010).</summary>
internal static class BrazilClock
{
    private static readonly TimeZoneInfo Zone =
        TimeZoneInfo.TryFindSystemTimeZoneById("America/Sao_Paulo", out var zone)
            ? zone
            : TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");

    public static DateTime ToLocal(DateTimeOffset moment) => TimeZoneInfo.ConvertTime(moment, Zone).DateTime;
}
