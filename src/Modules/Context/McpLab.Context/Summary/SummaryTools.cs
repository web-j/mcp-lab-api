using System.ComponentModel;
using System.Text.Json.Serialization;
using ModelContextProtocol.Server;

namespace McpLab.Context.Summary;

[JsonConverter(typeof(JsonStringEnumConverter<SummaryDetail>))]
public enum SummaryDetail
{
    [JsonStringEnumMemberName("brief")] Brief,
    [JsonStringEnumMemberName("full")] Full,
}

[McpServerToolType]
internal sealed class SummaryTools(ContextSummaryService summary)
{
    private readonly ContextSummaryService _summary = summary;

    [McpServerTool(Name = "context_get_summary", Title = "Onde parei",
        ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Cartão curto com onde a pessoa parou em qualquer app de IA: último "onde paramos", pendências e objetivos abertos
        (com o número de cada nota), últimas ações e resumo das semanas. Chame no começo da conversa, antes de responder
        sobre os dados dela, ou quando ela perguntar onde parou. Use detail=full só se o cartão disser que ficou algo de fora.
        """)]
    public async Task<ContextSummary> GetSummary(
        [Description("brief (padrão, até 1.500 caracteres) ou full (até 4.000)")] SummaryDetail detail = SummaryDetail.Brief,
        CancellationToken cancellationToken = default)
    {
        var (card, omitted) = await _summary.BuildAsync(detail == SummaryDetail.Full, cancellationToken);
        return new ContextSummary(card, omitted);
    }
}

[McpServerResourceType]
internal sealed class SummaryResources(ContextSummaryService summary)
{
    private readonly ContextSummaryService _summary = summary;

    [McpServerResource(UriTemplate = "context://summary", Name = "context_summary", Title = "Onde parei", MimeType = "text/plain")]
    [Description("O mesmo cartão de context_get_summary, para o app anexar à conversa.")]
    public async Task<string> Summary(CancellationToken cancellationToken) =>
        (await _summary.BuildAsync(full: false, cancellationToken)).Card;
}

[McpServerPromptType]
internal sealed class SummaryPrompts(ContextSummaryService summary)
{
    private readonly ContextSummaryService _summary = summary;

    [McpServerPrompt(Name = "context_resume", Title = "Continuar de onde parei")]
    [Description("Retoma a conversa de onde a pessoa parou, em qualquer app de IA.")]
    public async Task<string> Resume(CancellationToken cancellationToken)
    {
        var (card, _) = await _summary.BuildAsync(full: false, cancellationToken);
        return $"""
            Quero continuar de onde parei. Abaixo está o meu contexto salvo no MCP Lab.
            Resuma em até 3 frases onde eu estava e o que ficou pendente, e pergunte como quero seguir.

            {card}
            """;
    }
}

public sealed record ContextSummary(
    [property: Description("Cartão em linhas de texto, pronto para ler")] string Card,
    [property: Description("Quantos itens ficaram de fora por causa do teto")] int OmittedItems);
